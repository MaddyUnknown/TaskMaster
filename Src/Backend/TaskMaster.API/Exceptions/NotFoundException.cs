namespace TaskMaster.API.Exceptions;

public class NotFoundException : TaskMasterException
{
    public string EntityName { get; }
    public object? EntityKey { get; }

    public NotFoundException(string entityName)
        : base($"{entityName} not found.")
    {
        EntityName = entityName;
    }

    public NotFoundException(string entityName, object? entityKey)
        : base($"{entityName} with key '{entityKey}' not found.")
    {
        EntityName = entityName;
        EntityKey = entityKey;
    }
}
