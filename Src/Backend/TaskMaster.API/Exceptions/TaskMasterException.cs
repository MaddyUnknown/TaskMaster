namespace TaskMaster.API.Exceptions;

public abstract class TaskMasterException : Exception
{
    protected TaskMasterException()
    {
    }

    protected TaskMasterException(string? message) : base(message)
    {
    }

    protected TaskMasterException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
