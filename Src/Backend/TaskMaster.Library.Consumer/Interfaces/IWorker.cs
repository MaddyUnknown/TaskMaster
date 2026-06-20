namespace TaskMaster.Library.Consumer.Interfaces
{
    public interface IWorker : IAsyncDisposable
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
