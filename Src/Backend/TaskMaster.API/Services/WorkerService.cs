using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Mappers;
using TaskMaster.API.Models.Workers;

namespace TaskMaster.API.Services
{
    public class WorkerService : IWorkerService
    {
        private IOptions<WorkerConfig> _workerConfigOption;

        private IUnitOfWork _unitOfWork;
        private IRepository<Worker> _workerCRUDRepository;
        private IWorkerRepository _workerRepository;
        private IJobRepository _jobRepository;
        private IJobTypeRepository _jobTypeRepository;
        
        public WorkerService(IUnitOfWork unitOfWork, IRepository<Worker> workerCRUDRepository, IWorkerRepository workerRepository, IJobRepository jobRepository, IJobTypeRepository jobTypeRepository, IOptions<WorkerConfig> workerConfigOption)
        {
            _unitOfWork = unitOfWork;
            _workerCRUDRepository = workerCRUDRepository;
            _workerRepository = workerRepository;
            _jobRepository = jobRepository;
            _jobTypeRepository = jobTypeRepository;

            _workerConfigOption = workerConfigOption;
        }

        public async Task<RegisterWorkerResponse> RegisterAsync(RegisterWorker registerWorker)
        {
            var jobTypes = await _jobTypeRepository.GetByJobTypeNameAndVersionAsync(registerWorker.JobTypeCapabilities.Select(c => (c.Name, c.Version)));
            if (jobTypes.Count() != registerWorker.JobTypeCapabilities.Count()) throw new ValidationException("One or more job type capabilities do not exist.");

            var worker = registerWorker.ToWorker(jobTypes);
            _workerCRUDRepository.Add(worker);
            await _unitOfWork.SaveAsync();

            return worker.ToRegisterWorkerResponse(_workerConfigOption.Value.HeartBeatIntervalSeconds);
        }

        public async Task<WorkerDetails> RemoveAsync(Guid workerId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var worker = await _workerRepository.GetByPublicIdAsync(workerId);
                if (worker == null) throw new NotFoundException(nameof(Worker), workerId);

                worker.Status = WorkerStatusEnum.InActive;
                _workerCRUDRepository.Update(worker);
                await _unitOfWork.SaveAsync();

                await _jobRepository.UnassignJobForWorkerIdAsync(worker.Id);
                await _unitOfWork.SaveAsync();

                await _unitOfWork.CommitTransactionAsync();
                return worker.ToWorkerDetails();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<HeartbeatActionStatus> HeartBeatAsync(Guid workerId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var rowsAffected = await _workerRepository.UpdateWorkerExpiryTimestampAsync(workerId, _workerConfigOption.Value.WorkerExpiryIntervalSeconds);
                await _unitOfWork.SaveAsync();

                Worker? worker = null;
                if(rowsAffected != 0 ) worker = await _workerRepository.GetByPublicIdAsync(workerId);

                await _unitOfWork.CommitTransactionAsync();

                return new HeartbeatActionStatus
                {
                    ActionStatus = worker == null ? ActionStatusEnum.Failed : ActionStatusEnum.Ok
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
