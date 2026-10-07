namespace Test26.ApiR;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; } = null;
    public T? Date { get; set; }
    public List<string>? Errors { get; set; } = null;

    public static ApiResponse<T> Success(T data, string message = "Success")
        => new ApiResponse<T> {IsSuccess = true,Message = message,Date=data};

    public static ApiResponse<T> Failure(string message, List<string>? errors = null)
        => new ApiResponse<T> {IsSuccess = false, Message = message, Errors = errors};
}