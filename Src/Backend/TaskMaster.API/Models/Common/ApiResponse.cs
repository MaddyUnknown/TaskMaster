namespace TaskMaster.API.Models.Common;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public IList<string> ErrorMessages { get; set; } = new List<string>();

    public static ApiResponse<T> Success(T data) => new() { IsSuccess = true, Data = data };

    public static ApiResponse<T> Fail(string error) => new()
    {
        IsSuccess = false,
        ErrorMessages = new List<string> { error }
    };

    public static ApiResponse<T> Fail(IList<string> errors) => new()
    {
        IsSuccess = false,
        ErrorMessages = errors
    };
}
