using System.Diagnostics;
using TaskMaster.API.Interfaces.EventHandler;
using TaskMaster.API.Interfaces.Publisher;

namespace TaskMaster.API.Publishers
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EventPublisher> _logger;

        public EventPublisher(IServiceScopeFactory scopeFactory, ILogger<EventPublisher> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
        {
            var eventType = typeof(TEvent).Name;

            using var scope = _scopeFactory.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>().OrderBy(h => h.Order).ToList();

            if (handlers.Count == 0)
            {
                _logger.LogWarning("No handlers registered for event {EventType}", eventType);
                return;
            }

            foreach (var handler in handlers)
            {
                var handlerType = handler.GetType().Name;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    await handler.HandleAsync(@event, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event handler {HandlerType} failed while processing event {EventType}", handlerType, eventType);
                    continue;
                }

                stopwatch.Stop();
                _logger.LogInformation("Event handler {HandlerType} processed event {EventType} in {ElapsedMs}ms", handlerType, eventType, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
