namespace TaskMaster.API.Interfaces;

public interface IValidator<in T>
{
    IReadOnlyList<string> Validate(T instance);
}
