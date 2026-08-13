using TaskMaster.API.Constants;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Events;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Publisher;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Mappers;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.Workers;
using TaskMaster.API.Validation;

namespace TaskMaster.API.Services
{
    public class JobService : IJobService
    {
        private IUnitOfWork _unitOfWork;
        private IJobRepository _jobRepository;
        private IJobTypeRepository _jobTypeRepository;
        private IRepository<Job> _jobCRUDRepository;
        private IWorkerRepository _workerRepository;
        private IValidator<CreateJob> _createJobValidator;
        private IEventPublisher _eventPublisher;

        public JobService(IUnitOfWork unitOfWork, IRepository<Job> jobCRUDRepository, IJobTypeRepository jobTypeRepository, IJobRepository jobRepository, IWorkerRepository workerRepository, IValidator<CreateJob> createJobValidator, IEventPublisher eventPublisher)
        {
            _unitOfWork = unitOfWork;
            _jobRepository = jobRepository;
            _jobTypeRepository = jobTypeRepository;
            _jobCRUDRepository = jobCRUDRepository;
            _workerRepository = workerRepository;
            _createJobValidator = createJobValidator;
            _eventPublisher = eventPublisher;
        }

        public async Task<JobDetails> CreateAsync(CreateJob job)
        {
            var errors = _createJobValidator.Validate(job);
            if (errors.Count > 0) throw new ValidationException(errors);

            var jobTypeEntity = await _jobTypeRepository.GetByJobTypeNameAndVersionAsync(job.JobType.Name, job.JobType.Version);
            if (jobTypeEntity == null) throw new NotFoundException(nameof(JobType), (job.JobType.Name, job.JobType.Version));

            var jobEntity = job.ToJob(jobTypeEntity);
            
            _jobCRUDRepository.Add(jobEntity);
            await _unitOfWork.SaveAsync();

            await _eventPublisher.PublishAsync(new JobCreatedEvent
            {
                JobId = jobEntity.JobPublicId,
                JobTypeName = jobEntity.JobType.Name,
                JobTypeVersion = jobEntity.JobType.Version
            });

            return jobEntity.ToJobDetails();
        }

        public async Task<JobDetails> ChangeJobStatusAsync(Guid jobId, JobStatusEnum status, WorkerIdRef workerIdRef)
        {
            var error = new List<string>();
            if (jobId == Guid.Empty) error.Add(ErrorMessage.FieldRequired("JobId"));
            if (workerIdRef.WorkerId == Guid.Empty) error.Add(ErrorMessage.FieldRequired("WorkerId"));
            if (error.Count > 0) throw new ValidationException(error);

            var jobEntity = await _jobRepository.GetByJobPublicIdAndWorkerPublicIdAsync(jobId, workerIdRef.WorkerId);
            if (jobEntity == null) throw new NotFoundException(nameof(Job), jobId);

            jobEntity.Status = status;

            _jobCRUDRepository.Update(jobEntity);
            await _unitOfWork.SaveAsync();

            var worker = await _workerRepository.GetByPublicIdAsync(workerIdRef.WorkerId);
            await PublishJobStatusEventAsync(jobEntity, status, workerIdRef.WorkerId, worker?.WorkerName ?? string.Empty);

            return jobEntity.ToJobDetails();
        }

        public async Task<BulkUpdateJobStatusResponse> ChangeJobStatusAsync(BulkUpdateJobStatus request)
        {
            if (request.WorkerId == Guid.Empty) throw new ValidationException(ErrorMessage.FieldRequired("WorkerId"));

            var updatedRecord = 0;
            var errorMessages = new List<UpdateJobStatusErrorResponse>();
            var updatedJobs = new List<Job>();

            foreach(var item in request.JobStatuses)
            {
                var job = await _jobRepository.GetByJobPublicIdAndWorkerPublicIdAsync(item.JobId, request.WorkerId);
                if (job == null)
                {
                    errorMessages.Add(new UpdateJobStatusErrorResponse { JobId = item.JobId, ErrorReason = ErrorMessage.JobNotFound() });
                    continue;
                }

                job.Status = item.Status;
                _jobCRUDRepository.Update(job);
                updatedJobs.Add(job);
                updatedRecord++;
            }

            await _unitOfWork.SaveAsync();

            var worker = await _workerRepository.GetByPublicIdAsync(request.WorkerId);
            await PublishJobStatusEventAsync(updatedJobs, request.WorkerId, worker?.WorkerName ?? string.Empty);

            return new BulkUpdateJobStatusResponse { UpdatedRecordCount = updatedRecord, Errors = errorMessages };
        }

        public async Task<PagedResult<JobDetails>> GetAllJobsAsync(JobQuery query)
        {
            var errors = PaginationValidator.Validate(query);
            if (errors.Count > 0) throw new ValidationException(errors);

            var result = await _jobRepository.GetAllJobsAsync(query);
            return PagedResult<JobDetails>.Create(result.Items.Select(j => j.ToJobDetails()), result.Page, result.PageSize, result.TotalCount);
        }

        public async Task<JobDetails?> GetJobByPublicIdAsync(Guid jobId)
        {
            if (jobId == Guid.Empty) throw new ValidationException(ErrorMessage.FieldRequired("JobId"));
            var job = await _jobRepository.GetByJobPublicIdAsync(jobId);
            return job?.ToJobDetails();
        }

        public async Task<JobStatusCounts> GetJobStatusCountsAsync()
        {
            return await _jobRepository.CountJobsByStatusAsync();
        }

        public async Task<JobDetails?> GetNextWorkerJobsAsync(Guid workerId)
        {
            if (workerId == Guid.Empty) throw new ValidationException(ErrorMessage.FieldRequired("WorkerId"));

            var jobs = await GetNextWorkerJobsAsync(workerId, 1);
            return jobs.FirstOrDefault();
        }

        public async Task<IEnumerable<JobDetails>> GetNextWorkerJobsAsync(Guid workerId, int maxJobs)
        {
            if (workerId == Guid.Empty) throw new ValidationException(ErrorMessage.FieldRequired("WorkerId"));

            var jobs = await _jobRepository.GetNextJobsForWorkerAsync(workerId, maxJobs);

            if (jobs.Count > 0)
            {
                var worker = await _workerRepository.GetByPublicIdAsync(workerId);
                var @assignedJobEvents = jobs.Select(job => new JobAssignedEvent
                {
                    JobId = job.JobPublicId,
                    JobTypeName = job.JobType.Name,
                    JobTypeVersion = job.JobType.Version,
                    WorkerId = workerId,
                    WorkerName = worker?.WorkerName ?? string.Empty
                });

                await _eventPublisher.PublishAsync(assignedJobEvents);
            }

            return jobs.Select(j => j.ToJobDetails()).ToList();
        }

        private async Task PublishJobStatusEventAsync(Job job, JobStatusEnum status, Guid workerId, string workerName)
        {
            if (status == JobStatusEnum.Completed)
            {
                await _eventPublisher.PublishAsync(new JobCompletedEvent
                {
                    JobId = job.JobPublicId,
                    JobTypeName = job.JobType.Name,
                    JobTypeVersion = job.JobType.Version,
                    WorkerId = workerId,
                    WorkerName = workerName
                });
            }
            else if (status == JobStatusEnum.Failed)
            {
                await _eventPublisher.PublishAsync(new JobFailedEvent
                {
                    JobId = job.JobPublicId,
                    JobTypeName = job.JobType.Name,
                    JobTypeVersion = job.JobType.Version,
                    WorkerId = workerId,
                    WorkerName = workerName
                });
            }
        }

        private async Task PublishJobStatusEventAsync(IEnumerable<Job> updatedJobs, Guid workerId, string workerName)
        {
            var @jobCompleteEvents = updatedJobs.Where(job => job.Status == JobStatusEnum.Completed).Select(job =>
            {
                return new JobCompletedEvent
                {
                    JobId = job.JobPublicId,
                    JobTypeName = job.JobType.Name,
                    JobTypeVersion = job.JobType.Version,
                    WorkerId = workerId,
                    WorkerName = workerName
                };

                
            });

            await _eventPublisher.PublishAsync(@jobCompleteEvents);

            var @jobFailureEvents = updatedJobs.Where(job => job.Status == JobStatusEnum.Failed).Select(job =>
            {
                return new JobFailedEvent
                {
                    JobId = job.JobPublicId,
                    JobTypeName = job.JobType.Name,
                    JobTypeVersion = job.JobType.Version,
                    WorkerId = workerId,
                    WorkerName = workerName
                };
            });

            await _eventPublisher.PublishAsync(@jobFailureEvents);
        }
    }
}
