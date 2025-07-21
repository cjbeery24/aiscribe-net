namespace SermonTranscription.Application.Common;

/// <summary>
/// Generic service result wrapper for consistent error handling
/// </summary>
public class ServiceResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;

    private ServiceResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public static ServiceResult Success(string message = "Operation completed successfully") => new(true, message);
    public static ServiceResult Failure(string message) => new(false, message);
}

/// <summary>
/// Generic service result wrapper with data for consistent error handling
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public T? Data { get; private set; }

    private ServiceResult(bool isSuccess, string message, T? data = default)
    {
        IsSuccess = isSuccess;
        Message = message;
        Data = data;
    }

    public static ServiceResult<T> Success(T data, string message = "Operation completed successfully") => new(true, message, data);
    public static ServiceResult<T> Failure(string message) => new(false, message);
}
