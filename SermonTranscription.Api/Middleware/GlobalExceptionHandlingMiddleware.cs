using Microsoft.AspNetCore.Http.Extensions;
using SermonTranscription.Application.DTOs;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace SermonTranscription.Api.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var requestUrl = context.Request.GetDisplayUrl();
        var method = context.Request.Method;

        _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}, Method: {Method}, URL: {Url}",
            traceId, method, requestUrl);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = GetStatusCode(exception);

        var errorResponse = CreateErrorResponse(exception, traceId);
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private ErrorResponse CreateErrorResponse(Exception exception, string traceId)
    {
        var isDevelopment = _environment.IsDevelopment();

        return new ErrorResponse
        {
            Message = isDevelopment ? exception.Message : "An unexpected error occurred",
            Errors = isDevelopment
                ? [exception.Message, exception.StackTrace ?? "No stack trace available"]
                : ["Internal server error"],
            // Add trace ID for debugging
            // Note: In a real implementation, you might want to add this to a base ErrorResponse class
        };
    }
}
