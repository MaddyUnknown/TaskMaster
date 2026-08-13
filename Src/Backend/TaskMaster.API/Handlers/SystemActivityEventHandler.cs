using TaskMaster.API.Entities;
using TaskMaster.API.Enums;
using TaskMaster.API.Events;
using TaskMaster.API.Interfaces.Data;
using TaskMaster.API.Interfaces.EventHandler;
using TaskMaster.API.Interfaces.Repositories;

namespace TaskMaster.API.Handlers
{
    public class SystemActivityEventHandler :
        IEventHandler<JobCreatedEvent>,
        IEventHandler<JobAssignedEvent>,
        IEventHandler<IEnumerable<JobAssignedEvent>>,
        IEventHandler<JobCompletedEvent>,
        IEventHandler<IEnumerable<JobCompletedEvent>>,
        IEventHandler<JobFailedEvent>,
        IEventHandler<IEnumerable<JobFailedEvent>>,
        IEventHandler<WorkerRegisteredEvent>,
        IEventHandler<WorkerInactiveEvent>,
        IEventHandler<WorkerRemovedEvent>
    {
        private readonly IRepository<SystemActivity> _systemActivityRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SystemActivityEventHandler(IRepository<SystemActivity> systemActivityRepository, IUnitOfWork unitOfWork)
        {
            _systemActivityRepository = systemActivityRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task HandleAsync(JobCreatedEvent @event, CancellationToken cancellationToken = default)
        {
            await SaveAsync(
                EntityType.Job,
                @event.JobId,
                ActivityType.JobCreated,
                $"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' created");
        }

        public async Task HandleAsync(JobAssignedEvent @event, CancellationToken cancellationToken = default)
        {
            await SaveAsync(
                EntityType.Job,
                @event.JobId,
                ActivityType.JobAssigned,
                $"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' assigned to worker '{@event.WorkerName}'");
        }

        public async Task HandleAsync(IEnumerable<JobAssignedEvent> @events, CancellationToken cancellationToken = default)
        {
            foreach(var @event in @events)
            {
                var activity = new SystemActivity
                {
                    EntityType = EntityType.Job,
                    EntityId = @event.JobId,
                    ActivityType = ActivityType.JobAssigned,
                    Message = $"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' assigned to worker '{@event.WorkerName}'"
                };

                _systemActivityRepository.Add(activity);
            }
            
            await _unitOfWork.SaveAsync();
        }

        public async Task HandleAsync(JobCompletedEvent @event, CancellationToken cancellationToken = default)
        {
            await SaveAsync(
                EntityType.Job,
                @event.JobId,
                ActivityType.JobCompleted,
                $"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' completed by worker '{@event.WorkerName}'");
        }

        public async Task HandleAsync(IEnumerable<JobCompletedEvent> @events, CancellationToken cancellationToken = default)
        {
            foreach(var @event in @events)
            {
                var activity = new SystemActivity
                {
                    EntityType = EntityType.Job,
                    EntityId = @event.JobId,
                    ActivityType = ActivityType.JobCompleted,
                    Message = $"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' completed by worker '{@event.WorkerName}'"
                };

                _systemActivityRepository.Add(activity);
            }
           
            await _unitOfWork.SaveAsync();
        }

        public async Task HandleAsync(JobFailedEvent @event, CancellationToken cancellationToken = default)
        {
            await SaveAsync(
                EntityType.Job,
                @event.JobId,
                ActivityType.JobFailed,
                $"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' failed by worker '{@event.WorkerName}'");
        }

        public async Task HandleAsync(IEnumerable<JobFailedEvent> @events, CancellationToken cancellationToken = default)
        {
            foreach (var @event in @events)
            {
                var activity = new SystemActivity
                {
                    EntityType = EntityType.Job,
                    EntityId = @event.JobId,
                    ActivityType = ActivityType.JobFailed,
                    Message = $"Job '{@event.JobId}' of type '{@event.JobTypeName} (v{@event.JobTypeVersion})' failed by worker '{@event.WorkerName}'"
                };

                _systemActivityRepository.Add(activity);
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task HandleAsync(WorkerRegisteredEvent @event, CancellationToken cancellationToken = default)
        {
            await SaveAsync(
                EntityType.Worker,
                @event.WorkerId,
                ActivityType.WorkerRegistered,
                $"Worker '{@event.WorkerName}' registered");
        }

        public async Task HandleAsync(WorkerInactiveEvent @event, CancellationToken cancellationToken = default)
        {
            await SaveAsync(
                EntityType.Worker,
                @event.WorkerId,
                ActivityType.WorkerInactive,
                $"Worker '{@event.WorkerName}' deactivated (expired)");
        }

        public async Task HandleAsync(WorkerRemovedEvent @event, CancellationToken cancellationToken = default)
        {
            await SaveAsync(
                EntityType.Worker,
                @event.WorkerId,
                ActivityType.WorkerRemoved,
                $"Worker '{@event.WorkerName}' removed");
        }

        private async Task SaveAsync(EntityType entityType, Guid entityId, ActivityType activityType, string message)
        {
            var activity = new SystemActivity
            {
                EntityType = entityType,
                EntityId = entityId,
                ActivityType = activityType,
                Message = message
            };

            _systemActivityRepository.Add(activity);
            await _unitOfWork.SaveAsync();
        }
    }
}
