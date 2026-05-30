using System.Threading.Tasks;
using TaskMaster.API.Enums;
using TaskMaster.Entities;
using TaskMaster.Enums;
using TaskMaster.Interfaces.Data;
using TaskMaster.Interfaces.Repositories;
using TaskMaster.Interfaces.Services;
using TaskMaster.Mappers;
using TaskMaster.Models.Jobs;
using TaskMaster.Models.Workers;

namespace TaskMaster.Services
{
    public class JobService : IJobService
    {
        private IUnitOfWork _unitOfWork;
        private IJobRepository _jobRepository;
        private IRepository<Job> _jobCRUDRepository;
        private IWorkerRepository _workerRepository;

        public JobService(IUnitOfWork unitOfWork, IRepository<Job> jobCRUDRepository, IJobRepository jobRepository, IWorkerRepository workerRepository)
        {
            _unitOfWork = unitOfWork;
            _jobRepository = jobRepository;
            _jobCRUDRepository = jobCRUDRepository;
            _workerRepository = workerRepository;
        }

        public async Task<JobDetails> CreateAsync(JobCreateRequest job)
        {
            var jobEntity = job.ToJob();
            
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
