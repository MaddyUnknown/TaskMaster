namespace TaskMaster.API.Interfaces.EventHandler
{
    public interface IEventHandler<in TEvent> where TEvent : class
    {
        int Order => 0;

        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}
