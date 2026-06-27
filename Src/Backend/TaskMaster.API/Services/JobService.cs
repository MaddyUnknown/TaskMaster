using TaskMaster.API.Constants;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Mappers;
using TaskMaster.API.Models.Jobs;
using TaskMaster.API.Models.Workers;

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

        public JobService(IUnitOfWork unitOfWork, IRepository<Job> jobCRUDRepository, IJobTypeRepository jobTypeRepository, IJobRepository jobRepository, IWorkerRepository workerRepository, IValidator<CreateJob> createJobValidator)
        {
            _unitOfWork = unitOfWork;
            _jobRepository = jobRepository;
            _jobTypeRepository = jobTypeRepository;
            _jobCRUDRepository = jobCRUDRepository;
            _workerRepository = workerRepository;
            _createJobValidator = createJobValidator;
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

            return jobEntity.ToJobDetails();
        }

        public async Task<IEnumerable<JobDetails>> GetAllJobsAsync()
        {
            var jobs = await _jobRepository.GetAllJobsAsync();
            return jobs.Select(j => j.ToJobDetails());
        }

        public async Task<JobDetails?> GetJobByPublicIdAsync(Guid jobId)
        {
            if (jobId == Guid.Empty) throw new ValidationException(ErrorMessage.FieldRequired("JobId"));
            var job = await _jobRepository.GetByJobPublicIdAsync(jobId);
            return job?.ToJobDetails();
        }

        public async Task<JobDetails?> GetNextWorkerJobsAsync(Guid workerId)
        {
            if (workerId == Guid.Empty) throw new ValidationException(ErrorMessage.FieldRequired("WorkerId"));

            await _unitOfWork.BeginTransactionAsync();
            
            try
            {
                var worker = await _workerRepository.GetByPublicIdAsync(workerId);
                if (worker == null) throw new NotFoundException(nameof(Worker), workerId);
                if (worker.Status == WorkerStatusEnum.InActive) throw new WorkerInactiveException(workerId);


                var jobEntity = await _jobRepository.GetNextJobForWorkerAsync(worker.Id);
                await _unitOfWork.CommitTransactionAsync();
                return jobEntity?.ToJobDetails();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            
        }
    }
}
