using System.Diagnostics;
using System.Text;

namespace SermonTranscription.Api.Middleware;

/// <summary>
/// Middleware for logging HTTP requests and responses with correlation tracking
/// </summary>
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation ID for request tracking
        var correlationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        
        // Add correlation ID to response headers
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        var stopwatch = Stopwatch.StartNew();
        
        // Log incoming request
        await LogRequestAsync(context, correlationId);

        // Capture original response body stream
        var originalBodyStream = context.Response.Body;
        
        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Execute the next middleware in the pipeline
            await _next(context);

            // Log outgoing response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);

            // Copy the response back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
            stopwatch.Stop();
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;
        
        // Read request body for logging (if it exists and is not too large)
        string requestBody = string.Empty;
        if (request.ContentLength.HasValue && request.ContentLength > 0 && request.ContentLength < 10240) // 10KB limit
        {
            request.EnableBuffering();
            var buffer = new byte[request.ContentLength.Value];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            requestBody = Encoding.UTF8.GetString(buffer);
            request.Body.Position = 0; // Reset position for next middleware
        }

        _logger.LogInformation(
            "HTTP {Method} {Path} started. Correlation ID: {CorrelationId}. Content-Length: {ContentLength}. User: {User}",
            request.Method,
            $"{request.Path}{request.QueryString}",
            correlationId,
            request.ContentLength ?? 0,
            context.User?.Identity?.Name ?? "Anonymous"
        );

        // Log request body for POST/PUT requests (excluding sensitive endpoints)
        if (!string.IsNullOrEmpty(requestBody) && 
            (request.Method == "POST" || request.Method == "PUT") &&
            !IsSensitiveEndpoint(request.Path))
        {
            _logger.LogDebug(
                "Request Body for {CorrelationId}: {RequestBody}",
                correlationId,
                requestBody
            );
        }
    }

    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMs)
    {
        var response = context.Response;
        
        // Read response body for logging (if it's not too large)
        string responseBody = string.Empty;
        if (response.Body.CanSeek && response.Body.Length < 10240) // 10KB limit
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
        }

        var logLevel = GetLogLevelForStatusCode(response.StatusCode);
        
        _logger.Log(
            logLevel,
            "HTTP {Method} {Path} completed. Correlation ID: {CorrelationId}. Status: {StatusCode}. Duration: {ElapsedMs}ms",
            context.Request.Method,
            $"{context.Request.Path}{context.Request.QueryString}",
            correlationId,
            response.StatusCode,
            elapsedMs
        );

        // Log response body for errors or debug mode (excluding sensitive data)
        if (!string.IsNullOrEmpty(responseBody) && 
            (response.StatusCode >= 400 || _logger.IsEnabled(LogLevel.Debug)) &&
            !IsSensitiveEndpoint(context.Request.Path))
        {
            _logger.LogDebug(
                "Response Body for {CorrelationId}: {ResponseBody}",
                correlationId,
                responseBody
            );
        }
    }

    private static LogLevel GetLogLevelForStatusCode(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            >= 300 => LogLevel.Information,
            _ => LogLevel.Information
        };
    }

    private static bool IsSensitiveEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;
        
        return pathValue.Contains("/auth/login") ||
               pathValue.Contains("/auth/register") ||
               pathValue.Contains("/auth/reset-password") ||
               pathValue.Contains("/users/password") ||
               pathValue.Contains("/subscriptions/payment");
    }
} 