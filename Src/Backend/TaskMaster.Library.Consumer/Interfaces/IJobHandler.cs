namespace TaskMaster.Library.Consumer.Interfaces
{
    public interface IJobHandler
    {
        Type PayloadType { get; }
        Task HandleAsync(object payload, CancellationToken cancellationToken);
    }

    public interface IJobHandler<TPayload> : IJobHandler
    {
        Task HandleAsync(TPayload payload, CancellationToken cancellationToken);

        Type IJobHandler.PayloadType => typeof(TPayload);

        Task IJobHandler.HandleAsync(object payload, CancellationToken cancellationToken)
        {
            return HandleAsync((TPayload)payload, cancellationToken);
        }
    }
}
