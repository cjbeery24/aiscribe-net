using System;
using System.Collections.Generic;
using System.Linq;

namespace SermonTranscription.Application.Common;

/// <summary>
/// Predefined error codes for consistent error handling
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// Generic error when no specific error code applies
    /// </summary>
    GenericError,

    /// <summary>
    /// Resource not found
    /// </summary>
    NotFound,

    /// <summary>
    /// User is not authenticated
    /// </summary>
    Unauthorized,

    /// <summary>
    /// User is authenticated but lacks permission
    /// </summary>
    Forbidden,

    /// <summary>
    /// Resource conflict (e.g., duplicate email, name already exists)
    /// </summary>
    Conflict,

    /// <summary>
    /// Validation error (invalid input data)
    /// </summary>
    ValidationError,

    /// <summary>
    /// Internal server error
    /// </summary>
    InternalError
}

/// <summary>
/// Represents a detailed error in a service result
/// </summary>
public class ServiceError
{
    public string Message { get; set; } = string.Empty;
    public ErrorCode ErrorCode { get; set; }
    public string? Field { get; set; }
    public string? AttemptedValue { get; set; }

    public ServiceError(string message, ErrorCode errorCode = ErrorCode.GenericError, string? field = null, string? attemptedValue = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        ErrorCode = errorCode;
        Field = field;
        AttemptedValue = attemptedValue;
    }
}

/// <summary>
/// Generic service result wrapper for consistent error handling
/// </summary>
public class ServiceResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public IReadOnlyList<ServiceError> Errors { get; private set; } = Array.Empty<ServiceError>();

    protected ServiceResult(bool isSuccess, string message, IEnumerable<ServiceError>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message ?? string.Empty;
        Errors = errors?.ToList().AsReadOnly() ?? Array.Empty<ServiceError>().AsReadOnly();
    }

    public static ServiceResult Success(string message = "Operation completed successfully")
        => new(true, message);

    public static ServiceResult Failure(string message, IEnumerable<ServiceError>? errors = null)
        => new(false, message, errors);

    public static ServiceResult Failure(string message, ErrorCode errorCode, string? field = null, string? attemptedValue = null)
        => new(false, message, new[] { new ServiceError(message, errorCode, field, attemptedValue) });
}

/// <summary>
/// Generic service result wrapper with data for consistent error handling
/// </summary>
public class ServiceResult<T>
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public T? Data { get; private set; }
    public IReadOnlyList<ServiceError> Errors { get; private set; } = Array.Empty<ServiceError>();

    private ServiceResult(bool isSuccess, string message, T? data = default, IEnumerable<ServiceError>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message ?? string.Empty;
        Data = data;
        Errors = errors?.ToList().AsReadOnly() ?? Array.Empty<ServiceError>().AsReadOnly();
    }

    public static ServiceResult<T> Success(T data, string message = "Operation completed successfully")
        => new(true, message, data);

    public static ServiceResult<T> Failure(string message, IEnumerable<ServiceError>? errors = null)
        => new(false, message, default, errors);

    public static ServiceResult<T> Failure(string message, ErrorCode errorCode, string? field = null, string? attemptedValue = null)
        => new(false, message, default, new[] { new ServiceError(message, errorCode, field, attemptedValue) });
}
