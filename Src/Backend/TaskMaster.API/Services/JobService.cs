using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
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

        public JobService(IUnitOfWork unitOfWork, IRepository<Job> jobCRUDRepository, IJobTypeRepository jobTypeRepository, IJobRepository jobRepository, IWorkerRepository workerRepository)
        {
            _unitOfWork = unitOfWork;
            _jobRepository = jobRepository;
            _jobTypeRepository = jobTypeRepository;
            _jobCRUDRepository = jobCRUDRepository;
            _workerRepository = workerRepository;
        }

        public async Task<JobDetails> CreateAsync(CreateJob job)
        {
            var jobTypeEntity = await _jobTypeRepository.GetByJobTypeNameAndVersionAsync(job.JobType.Name, job.JobType.Version);
            if (jobTypeEntity == null) throw new Exception();

            var jobEntity = job.ToJob(jobTypeEntity);
            
            _jobCRUDRepository.Add(jobEntity);
            await _unitOfWork.SaveAsync();
            
            return jobEntity.ToJobDetails();
        }

        public async Task<JobDetails> ChangeJobStatusAsync(Guid jobId, JobStatusEnum status, WorkerIdRef workerIdRef)
        {
            var jobEntity = await _jobRepository.GetByJobPublicIdAndWorkerPublicIdAsync(jobId, workerIdRef.WorkerId);
            if(jobEntity == null) throw new Exception();

            jobEntity.Status = status;

            _jobCRUDRepository.Update(jobEntity);
            await _unitOfWork.SaveAsync();

            return jobEntity.ToJobDetails();
        }

        public async Task<JobDetails?> GetNextWorkerJobsAsync(Guid workerId)
        {
            await _unitOfWork.BeginTransactionAsync();
            
            try
            {
                var worker = await _workerRepository.GetByPublicIdAsync(workerId);
                if (worker == null || worker.Status == WorkerStatusEnum.InActive) throw new Exception(); 


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
