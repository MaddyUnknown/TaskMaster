namespace TaskMaster.Library.Consumer.Interfaces
{
    public interface IJobHandler
    {
        Type PayloadType { get; }
        Task HandleAsync(object payload);
    }

    public interface IJobHandler<TPayload> : IJobHandler
    {
        Task HandleAsync(TPayload payload);

        Type IJobHandler.PayloadType => typeof(TPayload);

        Task IJobHandler.HandleAsync(object payload)
        {
            return HandleAsync((TPayload)payload);
        }
    }
}
