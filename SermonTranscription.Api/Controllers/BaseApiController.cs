using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.DTOs;
using FluentValidation.Results;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Base API controller with standardized response and error handling for all controllers
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger _logger;

    protected BaseApiController(ILogger logger)
    {
        _logger = logger;
    }

    #region Protected Helper Methods

    /// <summary>
    /// Handles service results with data and returns appropriate HTTP responses using ApiSuccessResponse or ApiErrorResponse
    /// </summary>
    /// <typeparam name="T">Type of data in the service result</typeparam>
    /// <param name="result">Service result to handle</param>
    /// <param name="successAction">Action to execute on success</param>
    /// <returns>Appropriate HTTP response</returns>
    protected IActionResult HandleServiceResult<T>(ServiceResult<T> result, Func<IActionResult> successAction)
    {
        if (result.IsSuccess)
        {
            return successAction();
        }

        var validationErrors = result.Errors
            .Where(e => !string.IsNullOrEmpty(e.Field))
            .Select(e => new ValidationError
            {
                Field = e.Field!,
                Message = e.Message,
                ErrorCode = e.ErrorCode.ToString(),
                AttemptedValue = e.AttemptedValue
            })
            .ToArray();

        var errorResponse = ApiErrorResponse.Create(
            message: result.Message,
            errors: result.Errors.Select(e => e.Message).ToArray(),
            validationErrors: validationErrors
        );
        errorResponse.TraceId = HttpContext.TraceIdentifier;

        if (validationErrors.Any())
        {
            return BadRequest(errorResponse);
        }

        var errorCode = result.Errors.FirstOrDefault()?.ErrorCode;
        switch (errorCode)
        {
            case ErrorCode.NotFound:
            case var _ when result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase):
                return NotFound(errorResponse);

            case ErrorCode.Unauthorized:
            case var _ when result.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                           result.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase):
                return Unauthorized(errorResponse);

            case ErrorCode.Forbidden:
            case var _ when result.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase):
                return StatusCode(403, errorResponse);

            case ErrorCode.Conflict:
            case var _ when result.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase):
                return Conflict(errorResponse);

            default:
                return BadRequest(errorResponse);
        }
    }

    /// <summary>
    /// Handles service results without data and returns appropriate HTTP responses using ApiSuccessResponse or ApiErrorResponse
    /// </summary>
    /// <param name="result">Service result to handle</param>
    /// <param name="successAction">Action to execute on success</param>
    /// <returns>Appropriate HTTP response</returns>
    protected IActionResult HandleServiceResult(ServiceResult result, Func<IActionResult> successAction)
    {
        if (result.IsSuccess)
        {
            return successAction();
        }

        var validationErrors = result.Errors
            .Where(e => !string.IsNullOrEmpty(e.Field))
            .Select(e => new ValidationError
            {
                Field = e.Field!,
                Message = e.Message,
                ErrorCode = e.ErrorCode.ToString(),
                AttemptedValue = e.AttemptedValue
            })
            .ToArray();

        var errorResponse = ApiErrorResponse.Create(
            message: result.Message,
            errors: result.Errors.Select(e => e.Message).ToArray(),
            validationErrors: validationErrors
        );
        errorResponse.TraceId = HttpContext.TraceIdentifier;

        if (validationErrors.Any())
        {
            return BadRequest(errorResponse);
        }

        var errorCode = result.Errors.FirstOrDefault()?.ErrorCode;
        switch (errorCode)
        {
            case ErrorCode.NotFound:
            case var _ when result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase):
                return NotFound(errorResponse);

            case ErrorCode.Unauthorized:
            case var _ when result.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                           result.Message.Contains("access denied", StringComparison.OrdinalIgnoreCase):
                return Unauthorized(errorResponse);

            case ErrorCode.Forbidden:
            case var _ when result.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase):
                return StatusCode(403, errorResponse);

            case ErrorCode.Conflict:
            case var _ when result.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase):
                return Conflict(errorResponse);

            default:
                return BadRequest(errorResponse);
        }
    }

    /// <summary>
    /// Validates the model state and FluentValidation results, returning a standardized validation error response if invalid
    /// </summary>
    /// <param name="validationResult">FluentValidation result, if any</param>
    /// <returns>Validation error response or null if valid</returns>
    protected IActionResult? ValidateRequest(ValidationResult? validationResult = null)
    {
        // Check FluentValidation result if provided
        if (validationResult != null && !validationResult.IsValid)
        {
            var validationErrors = validationResult.Errors.Select(e => new ValidationError
            {
                Field = e.PropertyName,
                Message = e.ErrorMessage,
                ErrorCode = e.ErrorCode,
                AttemptedValue = e.AttemptedValue?.ToString()
            }).ToArray();

            var errorResponse = ApiErrorResponse.Create(
                message: "Validation failed",
                errors: validationErrors.Select(e => e.Message).ToArray(),
                validationErrors: validationErrors
            );
            errorResponse.TraceId = HttpContext.TraceIdentifier;

            return BadRequest(errorResponse);
        }

        // Check ModelState
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new ValidationError
                {
                    Field = ModelState.Keys.FirstOrDefault(k => ModelState[k]?.Errors.Contains(e) == true) ?? "Unknown",
                    Message = e.ErrorMessage ?? "Unknown validation error",
                    ErrorCode = "VALIDATION_ERROR",
                    AttemptedValue = ModelState.Values.FirstOrDefault(v => v.Errors.Contains(e))?.AttemptedValue
                })
                .ToArray();

            var errorResponse = ApiErrorResponse.Create(
                message: "Validation failed",
                errors: validationErrors.Select(e => e.Message).ToArray(),
                validationErrors: validationErrors
            );
            errorResponse.TraceId = HttpContext.TraceIdentifier;

            return BadRequest(errorResponse);
        }

        return null;
    }

    /// <summary>
    /// Creates a success response with data using ApiSuccessResponse wrapper
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    /// <param name="data">Data to return</param>
    /// <param name="message">Success message</param>
    /// <returns>Success response</returns>
    protected IActionResult SuccessResponse<T>(T data, string message = "Operation completed successfully")
    {
        var response = ApiSuccessResponse<T>.Create(data, message);
        response.TraceId = HttpContext.TraceIdentifier;
        return Ok(response);
    }

    /// <summary>
    /// Creates a success response without data using ApiSuccessResponse wrapper
    /// </summary>
    /// <param name="message">Success message</param>
    /// <returns>Success response</returns>
    protected IActionResult SuccessResponse(string message = "Operation completed successfully")
    {
        var response = ApiSuccessResponse.Create(message);
        response.TraceId = HttpContext.TraceIdentifier;
        return Ok(response);
    }

    /// <summary>
    /// Creates a 201 Created response with data using ApiSuccessResponse wrapper
    /// </summary>
    /// <typeparam name="T">Type of data being returned</typeparam>
    /// <param name="data">Data to return</param>
    /// <param name="message">Success message</param>
    /// <returns>201 Created response</returns>
    protected IActionResult CreatedResponse<T>(T data, string message = "Resource created successfully")
    {
        var response = ApiSuccessResponse<T>.Create(data, message);
        response.TraceId = HttpContext.TraceIdentifier;
        return StatusCode(201, response);
    }

    #endregion
}
