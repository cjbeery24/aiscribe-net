using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.DTOs;
using System.Security.Claims;

namespace SermonTranscription.Api.Controllers;

/// <summary>
/// Base API controller with common functionality for all controllers
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
    /// Validates the model state and returns an error response if invalid
    /// </summary>
    /// <returns>UnprocessableEntity result if validation fails, null if valid</returns>
    protected IActionResult? ValidateModelState()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return UnprocessableEntity(new ErrorResponse
            {
                Message = "Invalid request data",
                Errors = errors.ToArray()
            });
        }
        return null;
    }

    /// <summary>
    /// Handles service results with data and returns appropriate HTTP responses
    /// </summary>
    /// <typeparam name="T">Type of data in the service result</typeparam>
    /// <param name="result">Service result to handle</param>
    /// <param name="successAction">Action to execute on success</param>
    /// <returns>Appropriate HTTP response</returns>
    protected IActionResult HandleServiceResult<T>(ServiceResult<T> result, Func<IActionResult> successAction)
    {
        if (!result.IsSuccess)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = [result.Message]
                });
            }

            return BadRequest(new ErrorResponse
            {
                Message = result.Message,
                Errors = [result.Message]
            });
        }

        return successAction();
    }

    /// <summary>
    /// Handles service results without data and returns appropriate HTTP responses
    /// </summary>
    /// <param name="result">Service result to handle</param>
    /// <param name="successAction">Action to execute on success</param>
    /// <returns>Appropriate HTTP response</returns>
    protected IActionResult HandleServiceResult(ServiceResult result, Func<IActionResult> successAction)
    {
        if (!result.IsSuccess)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(new ErrorResponse
                {
                    Message = result.Message,
                    Errors = [result.Message]
                });
            }

            return BadRequest(new ErrorResponse
            {
                Message = result.Message,
                Errors = [result.Message]
            });
        }

        return successAction();
    }

    /// <summary>
    /// Handles exceptions with consistent logging and error responses
    /// </summary>
    /// <param name="ex">Exception that occurred</param>
    /// <param name="logMessage">Message to log</param>
    /// <returns>Internal server error response</returns>
    protected IActionResult HandleException(Exception ex, string logMessage)
    {
        _logger.LogError(ex, logMessage);
        return StatusCode(500, new ErrorResponse
        {
            Message = "An error occurred while processing the request",
            Errors = ["Internal server error"]
        });
    }



    /// <summary>
    /// Creates a standardized error response for bad request
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errors">Additional error details</param>
    /// <returns>Bad request response</returns>
    protected IActionResult BadRequestError(string message, string[]? errors = null)
    {
        return BadRequest(new ErrorResponse
        {
            Message = message,
            Errors = errors ?? [message]
        });
    }

    #endregion
}
