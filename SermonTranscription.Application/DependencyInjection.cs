using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace SermonTranscription.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper Configuration
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // FluentValidation Configuration
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Application Service Registrations (will be implemented later)
        // services.AddScoped<IAuthService, AuthService>();
        // services.AddScoped<ITranscriptionService, TranscriptionService>();
        // services.AddScoped<IOrganizationService, OrganizationService>();

        return services;
    }
} 