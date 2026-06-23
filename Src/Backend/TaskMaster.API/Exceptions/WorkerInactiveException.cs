namespace TaskMaster.API.Exceptions;

public class WorkerInactiveException : TaskMasterException
{
    public Guid WorkerId { get; }

    public WorkerInactiveException(Guid workerId)
        : base($"Worker '{workerId}' is inactive.")
    {
        WorkerId = workerId;
    }
}
