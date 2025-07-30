namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Generic API success response wrapper for consistent success response structure
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiSuccessResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Operation completed successfully";
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public string TraceId { get; set; } = string.Empty;

    public static ApiSuccessResponse<T> Create(T data, string message = "Operation completed successfully")
    {
        return new ApiSuccessResponse<T>
        {
            Data = data,
            Message = message
        };
    }
}

/// <summary>
/// Non-generic API success response wrapper for operations that don't return data
/// </summary>
public class ApiSuccessResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Operation completed successfully";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public string TraceId { get; set; } = string.Empty;

    public static ApiSuccessResponse Create(string message = "Operation completed successfully")
    {
        return new ApiSuccessResponse
        {
            Message = message
        };
    }
}

/// <summary>
/// API error response wrapper for consistent error response structure
/// </summary>
public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = "An error occurred";
    public string[]? Errors { get; set; }
    public ValidationError[] ValidationErrors { get; set; } = Array.Empty<ValidationError>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public string TraceId { get; set; } = string.Empty;

    public static ApiErrorResponse Create(string message, string[]? errors = null, ValidationError[]? validationErrors = null)
    {
        return new ApiErrorResponse
        {
            Message = message,
            Errors = errors ?? (validationErrors?.Any() == true ? validationErrors.Select(e => e.Message).ToArray() : [message]),
            ValidationErrors = validationErrors ?? Array.Empty<ValidationError>()
        };
    }
}
