using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SermonTranscription.Application.Services;
using SermonTranscription.Application.Interfaces;

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
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ITranscriptionSessionService, TranscriptionSessionService>();
        services.AddScoped<IAudioStreamService, AudioStreamService>();

        services.AddScoped<InvitationService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPasswordValidator, PasswordValidator>();

        return services;
    }
}
