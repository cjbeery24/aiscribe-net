using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.IdentityModel.Tokens;
using SermonTranscription.Api.Authorization;
using SermonTranscription.Api.Middleware;
using SermonTranscription.Application;
using SermonTranscription.Infrastructure;
using Serilog;
using System.Text;

// Create initial configuration to read Serilog settings
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Ensure logs directory exists
Directory.CreateDirectory("logs");

// Configure Serilog from configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Starting Sermon Transcription API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog as the logging provider and configure it to read from config
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container

    // Layer Dependencies
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Controllers and API Explorer
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Swagger/OpenAPI Configuration
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "Sermon Transcription API",
            Version = "v1",
            Description = "REST API for live sermon transcription service"
        });

        // JWT Authentication in Swagger
        c.AddSecurityDefinition("Bearer", new()
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new()
        {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
        });

        // Add X-Organization-ID header parameter for all operations
        c.AddSecurityDefinition("X-Organization-ID", new()
        {
            Description = "Organization ID header for multi-tenant requests. Required for most endpoints that operate within an organization context.",
            Name = "X-Organization-ID",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
        });

        // Add global parameter for X-Organization-ID
        c.AddSecurityRequirement(new()
        {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "X-Organization-ID" }
            },
            new string[] {}
        }
        });
    });

    // JWT Authentication Configuration
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    // Only configure JWT authentication if SecretKey is provided
    if (!string.IsNullOrEmpty(secretKey))
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.Zero
                };

                // Configure JWT in SignalR
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
    }
    else
    {
        // For testing environments, skip authentication configuration
        // This allows the application to start without JWT configuration
    }

    // Authorization
    builder.Services.AddAuthorization(options =>
    {
        // Organization Admin Policy - requires admin role (validated by TenantMiddleware)
        options.AddPolicy(AuthorizationPolicies.OrganizationAdmin, policy =>
            policy.RequireAuthenticatedUser()
                   .RequireAssertion(context => context.Resource is HttpContext httpContext &&
                                               httpContext.IsAdmin()));

        // Organization User Policy - requires user or admin role
        options.AddPolicy(AuthorizationPolicies.OrganizationUser, policy =>
            policy.RequireAuthenticatedUser()
                   .RequireAssertion(context => context.Resource is HttpContext httpContext &&
                                               httpContext.CanManageTranscriptions()));

        // Read Only User Policy - requires any valid role
        options.AddPolicy(AuthorizationPolicies.ReadOnlyUser, policy =>
            policy.RequireAuthenticatedUser()
                   .RequireAssertion(context => context.Resource is HttpContext httpContext &&
                                               httpContext.CanViewTranscriptions()));

        // Can Manage Users Policy
        options.AddPolicy(AuthorizationPolicies.CanManageUsers, policy =>
            policy.RequireAuthenticatedUser()
                   .RequireAssertion(context => context.Resource is HttpContext httpContext &&
                                               httpContext.CanManageUsers()));

        // Can Manage Transcriptions Policy
        options.AddPolicy(AuthorizationPolicies.CanManageTranscriptions, policy =>
            policy.RequireAuthenticatedUser()
                   .RequireAssertion(context => context.Resource is HttpContext httpContext &&
                                               httpContext.CanManageTranscriptions()));

        // Can View Transcriptions Policy
        options.AddPolicy(AuthorizationPolicies.CanViewTranscriptions, policy =>
            policy.RequireAuthenticatedUser()
                   .RequireAssertion(context => context.Resource is HttpContext httpContext &&
                                               httpContext.CanViewTranscriptions()));

        // Can Export Transcriptions Policy
        options.AddPolicy(AuthorizationPolicies.CanExportTranscriptions, policy =>
            policy.RequireAuthenticatedUser()
                   .RequireAssertion(context => context.Resource is HttpContext httpContext &&
                                               httpContext.CanExportTranscriptions()));





        // Authenticated User Policy - any valid JWT token
        options.AddPolicy(AuthorizationPolicies.AuthenticatedUser, policy =>
            policy.RequireAuthenticatedUser());
    });
    builder.Services.AddHttpContextAccessor();

    // API Versioning
    builder.Services.AddApiVersioning(opt =>
    {
        opt.DefaultApiVersion = new ApiVersion(1, 0);
        opt.AssumeDefaultVersionWhenUnspecified = true;
        opt.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new QueryStringApiVersionReader("apiVersion"),
            new HeaderApiVersionReader("X-Version"),
            new MediaTypeApiVersionReader("ver")
        );
    });

    // SignalR
    builder.Services.AddSignalR();

    // CORS Configuration
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required for SignalR
        });
    });

    // FluentValidation
    builder.Services.AddFluentValidationAutoValidation()
                    .AddFluentValidationClientsideAdapters();

    // Health Checks
    builder.Services.AddHealthChecks();

    // Build the application
    var app = builder.Build();

    // Configure the HTTP request pipeline

    // Development Environment
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sermon Transcription API V1");
            c.RoutePrefix = string.Empty; // Serve Swagger UI at root
        });
    }

    // Security Headers
    app.UseHttpsRedirection();

    // Request/Response Logging (before authentication to capture all requests)
    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    // CORS
    app.UseCors("AllowFrontend");

    // Authentication & Authorization
    app.UseAuthentication();

    // Helper function to determine if authentication middleware should be applied
    static bool ShouldApplyAuthenticationMiddleware(HttpContext context)
    {
        // Skip middleware for public endpoints
        var endpoint = context.GetEndpoint();
        if (endpoint == null) return false;

        // Check for PublicEndpoint attribute
        var publicAttribute = endpoint.Metadata.GetMetadata<PublicEndpointAttribute>();
        if (publicAttribute != null) return false;

        // Check for PublicEndpoint attribute on controller
        var controllerAttribute = endpoint.Metadata.GetMetadata<PublicEndpointAttribute>();
        if (controllerAttribute != null) return false;

        // Skip for health checks and swagger
        var pathValue = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (pathValue.StartsWith("/health") ||
            pathValue.StartsWith("/swagger") ||
            pathValue.StartsWith("/api-docs"))
            return false;

        // Apply authentication middleware to all other routes
        return true;
    }

    // Helper function to determine if tenant middleware should be applied
    static bool ShouldApplyTenantMiddleware(HttpContext context)
    {
        // Skip tenant middleware for public endpoints
        var endpoint = context.GetEndpoint();
        if (endpoint == null) return false;

        // Check for PublicEndpoint attribute
        var publicAttribute = endpoint.Metadata.GetMetadata<PublicEndpointAttribute>();
        if (publicAttribute != null) return false;

        // Check for PublicEndpoint attribute on controller
        var controllerAttribute = endpoint.Metadata.GetMetadata<PublicEndpointAttribute>();
        if (controllerAttribute != null) return false;

        // Skip for health checks and swagger
        var pathValue = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        if (pathValue.StartsWith("/health") ||
            pathValue.StartsWith("/swagger") ||
            pathValue.StartsWith("/api-docs"))
            return false;

        // Skip tenant middleware for organization-agnostic endpoints
        var agnosticAttribute = endpoint.Metadata.GetMetadata<OrganizationAgnosticAttribute>();
        if (agnosticAttribute != null) return false;

        var agnosticControllerAttribute = endpoint.Metadata.GetMetadata<OrganizationAgnosticAttribute>();
        if (agnosticControllerAttribute != null) return false;

        // Apply tenant middleware to organization-specific routes only
        return true;
    }

    // Authentication middleware (only for non-public endpoints)
    app.UseWhen(ShouldApplyAuthenticationMiddleware, appBuilder =>
    {
        appBuilder.UseMiddleware<SermonTranscription.Api.Middleware.AuthenticationMiddleware>();
    });

    // Multi-tenant middleware (only for organization-specific endpoints)
    app.UseWhen(ShouldApplyTenantMiddleware, appBuilder =>
    {
        appBuilder.UseMiddleware<TenantMiddleware>();
    });

    app.UseAuthorization();

    // Controllers
    app.MapControllers();

    // SignalR Hubs (will be implemented later)
    // app.MapHub<TranscriptionHub>("/hubs/transcription");

    // Health Checks
    app.MapHealthChecks("/health");

    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down Sermon Transcription API");
    Log.CloseAndFlush();
}

// Make Program class accessible for testing
public partial class Program { }
