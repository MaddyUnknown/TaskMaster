namespace TaskMaster.API.Exceptions;

public class ValidationException : TaskMasterException
{
    public IReadOnlyList<string> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new List<string> { message }.AsReadOnly();
    }

    public ValidationException(IEnumerable<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = new List<string>(errors).AsReadOnly();
    }
}
