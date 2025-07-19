using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SermonTranscription.Application.Services;

namespace SermonTranscription.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper Configuration
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        // FluentValidation Configuration
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Service registrations
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
} 