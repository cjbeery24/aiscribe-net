using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SermonTranscription.Application.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace SermonTranscription.Api.Swagger;

/// <summary>
/// Customizes Swagger response examples for ApiErrorResponse to ensure accurate error response examples
/// </summary>
public class ApiErrorResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var response in operation.Responses)
        {
            // Check for error status codes
            if (response.Key is "400" or "401" or "403" or "404" or "409" or "500")
            {
                // Check all content types (not just application/json)
                foreach (var content in response.Value.Content)
                {
                    var schema = content.Value.Schema;
                    // Check if the schema corresponds to ApiErrorResponse
                    if (schema?.Reference?.Id == nameof(ApiErrorResponse) ||
                        context.SchemaRepository.Schemas.Any(s => s.Key == schema?.Reference?.Id && s.Value.Type == typeof(ApiErrorResponse).Name))
                    {
                        content.Value.Example = new OpenApiObject
                        {
                            ["success"] = new OpenApiBoolean(false),
                            ["message"] = new OpenApiString("string"),
                            ["errors"] = new OpenApiArray { new OpenApiString("string") },
                            ["validationErrors"] = new OpenApiArray {
                                new OpenApiObject {
                                    ["field"] = new OpenApiString("string"),
                                    ["message"] = new OpenApiString("string"),
                                    ["errorCode"] = new OpenApiString("string"),
                                    ["attemptedValue"] = new OpenApiString("string"),
                                }
                            },
                            ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("o")),
                            ["requestId"] = new OpenApiString("string"),
                            ["traceId"] = new OpenApiString("string")
                        };
                    }
                }
            }
        }
    }
}
