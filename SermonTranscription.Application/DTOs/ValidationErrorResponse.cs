namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Detailed validation error response with field-specific error information
/// </summary>
public class ValidationErrorResponse : ErrorResponse
{
    public ValidationError[] ValidationErrors { get; set; } = Array.Empty<ValidationError>();
    public string TraceId { get; set; } = string.Empty;
}

/// <summary>
/// Individual validation error for a specific field
/// </summary>
public class ValidationError
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public object? AttemptedValue { get; set; }
}
