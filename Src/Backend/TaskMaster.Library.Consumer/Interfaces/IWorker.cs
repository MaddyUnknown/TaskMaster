namespace TaskMaster.Library.Consumer.Interfaces
{
    public interface IWorker
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
