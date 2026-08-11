using Microsoft.Extensions.Options;
using TaskMaster.API.Configs;
using TaskMaster.API.Constants;
using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Events;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Models.Enums;
using TaskMaster.API.Interfaces;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.Publisher;
using TaskMaster.API.Interfaces.Repositories;
using TaskMaster.API.Interfaces.Services;
using TaskMaster.API.Mappers;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.Workers;
using TaskMaster.API.Validation;

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
        private IValidator<RegisterWorker> _registerWorkerValidator;
        private IEventPublisher _eventPublisher;

        public WorkerService(IUnitOfWork unitOfWork, IRepository<Worker> workerCRUDRepository, IWorkerRepository workerRepository, IJobRepository jobRepository, IJobTypeRepository jobTypeRepository, IOptions<WorkerConfig> workerConfigOption, IValidator<RegisterWorker> registerWorkerValidator, IEventPublisher eventPublisher)
        {
            _unitOfWork = unitOfWork;
            _workerCRUDRepository = workerCRUDRepository;
            _workerRepository = workerRepository;
            _jobRepository = jobRepository;
            _jobTypeRepository = jobTypeRepository;

            _workerConfigOption = workerConfigOption;
            _registerWorkerValidator = registerWorkerValidator;
            _eventPublisher = eventPublisher;
        }

        public async Task<RegisterWorkerResponse> RegisterAsync(RegisterWorker registerWorker)
        {
            var errors = _registerWorkerValidator.Validate(registerWorker);
            if (errors.Count > 0) throw new ValidationException(errors);

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var worker = await _workerRepository.GetByWorkerNameAsync(registerWorker.WorkerName, withLock: true);
                if (worker != null && worker.Status == WorkerStatusEnum.Active) throw new ValidationException(ErrorMessage.ActiveWorkerAlreadyExists(registerWorker.WorkerName));

                var jobTypes = await _jobTypeRepository.GetByJobTypeNameAndVersionAsync(registerWorker.JobTypeCapabilities.Select(c => (c.Name, c.Version)));
                if (jobTypes.Count() != registerWorker.JobTypeCapabilities.Count()) throw new ValidationException(ErrorMessage.OneOrMoreJobTypeCapabilitiesDoNotExist());

                if (worker == null)
                {
                    worker = registerWorker.ToWorker(jobTypes, _workerConfigOption.Value.WorkerExpiryIntervalSeconds);
                    _workerCRUDRepository.Add(worker);
                }
                else
                {
                    await _jobRepository.UnassignJobForWorkerIdAsync(worker.Id);
                    worker.WorkerCapabilities.Clear();
                    foreach (var jobType in jobTypes)
                    {
                        worker.WorkerCapabilities.Add(new WorkerCapability { JobType = jobType });
                    }

                    worker.LastHeartBeatTimestamp = DateTime.Now;
                    worker.WorkerExpiresAtTimestamp = DateTime.Now.AddSeconds(_workerConfigOption.Value.WorkerExpiryIntervalSeconds);
                    worker.Status = WorkerStatusEnum.Active;
                    _workerCRUDRepository.Update(worker);
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _eventPublisher.PublishAsync(new WorkerRegisteredEvent
                {
                    WorkerId = worker.WorkerPublicId,
                    WorkerName = worker.WorkerName
                });

                return worker.ToRegisterWorkerResponse(_workerConfigOption.Value.HeartBeatIntervalSeconds);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
            
        }

        public async Task<WorkerDetails> RemoveAsync(Guid workerId)
        {
            if (workerId == Guid.Empty) throw new ValidationException(ErrorMessage.FieldRequired("WorkerId"));

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

                await _eventPublisher.PublishAsync(new WorkerRemovedEvent
                {
                    WorkerId = worker.WorkerPublicId,
                    WorkerName = worker.WorkerName
                });

                return worker.ToWorkerDetails();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PagedResult<WorkerDetails>> GetAllWorkersAsync(WorkerQuery query)
        {
            var errors = PaginationValidator.Validate(query);
            if (errors.Count > 0) throw new ValidationException(errors);

            var result = await _workerRepository.GetAllWorkersAsync(query);
            return PagedResult<WorkerDetails>.Create(result.Items.Select(w => w.ToWorkerDetails()), result.Page, result.PageSize, result.TotalCount);
        }

        public async Task<WorkerDetails?> GetWorkerByPublicIdAsync(Guid workerId)
        {
            if (workerId == Guid.Empty) throw new ValidationException(ErrorMessage.FieldRequired("WorkerId"));
            var worker = await _workerRepository.GetByPublicIdAsync(workerId);
            return worker?.ToWorkerDetails();
        }

        public async Task<WorkerStatusCounts> GetWorkerStatusCountsAsync()
        {
            return await _workerRepository.CountWorkersByStatusAsync();
        }

        public async Task<HeartbeatActionStatus> HeartBeatAsync(Guid workerId)
        {
            if (workerId == Guid.Empty) throw new ValidationException(ErrorMessage.FieldRequired("WorkerId"));

            var worker = await _workerRepository.UpdateWorkerExpiryAndReturnAsync(workerId, _workerConfigOption.Value.WorkerExpiryIntervalSeconds);

            return new HeartbeatActionStatus
            {
                ActionStatus = worker == null ? ActionStatusEnum.Failed : ActionStatusEnum.Ok
            };
        }
    }
}
