using Microsoft.AspNetCore.Mvc;
using TaskMaster.Enums;
using TaskMaster.Entities;
using TaskMaster.Interfaces.Data;
using TaskMaster.Interfaces.Repositories;
using TaskMaster.Interfaces.Services;
using TaskMaster.Mappers;
using TaskMaster.Models.Workers;
using TaskMaster.API.Enums;
using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;

namespace TaskMaster.Services
{
    public class WorkerService : IWorkerService
    {
        private IOptions<WorkerConfig> _workerConfigOption;

        private IUnitOfWork _unitOfWork;
        private IRepository<Worker> _workerCRUDRepository;
        private IWorkerRepository _workerRepository;
        private IJobRepository _jobRepository;
        
        public WorkerService(IUnitOfWork unitOfWork, IRepository<Worker> workerCRUDRepository, IWorkerRepository workerRepository, IJobRepository jobRepository, IOptions<WorkerConfig> workerConfigOption)
        {
            _unitOfWork = unitOfWork;
            _workerCRUDRepository = workerCRUDRepository;
            _workerRepository = workerRepository;
            _jobRepository = jobRepository;

            _workerConfigOption = workerConfigOption;
        }

        public async Task<RegisterWorkerResponse> RegisterAsync(RegisterWorkerRequest registerWorker)
        {
            var worker = registerWorker.ToWorker();
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
                if (worker == null) throw new Exception();

                worker.Status = WorkerStatusEnum.InActive;
                _workerCRUDRepository.Update(worker);
                await _unitOfWork.SaveAsync();

                await _jobRepository.UnassignJobForWorkerId(worker.Id);
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

        public async Task<ActionStatusResponse> HeartBeatAsync(Guid workerId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var worker = await _workerRepository.UpdateWorkerExpiryTimestampAsync(workerId, _workerConfigOption.Value.WorkerExpiryIntervalSeconds);

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                return new ActionStatusResponse
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
