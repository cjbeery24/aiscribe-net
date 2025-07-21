using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Infrastructure.Configuration;
using SermonTranscription.Infrastructure.Services;

namespace SermonTranscription.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database Configuration - conditionally register based on environment
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (environment == "Test" || connectionString?.Contains(":memory:") == true)
        {
            // Use SQLite in-memory database for testing
            services.AddDbContext<Data.AppDbContext>(options =>
            {
                options.UseSqlite(connectionString);
                options.EnableSensitiveDataLogging();
            });
        }
        else
        {
            // Use PostgreSQL for production/development
            services.AddDbContext<Data.AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        // JWT Configuration
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<IJwtService, JwtService>();

        // Redis Configuration
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configuration = provider.GetService<IConfiguration>();
            var redisConnectionString = configuration?.GetConnectionString("Redis") ?? "localhost:6379";
            return ConnectionMultiplexer.Connect(redisConnectionString);
        });

        // Repository registrations
        services.AddScoped<IUserRepository, Repositories.UserRepository>();
        // services.AddScoped<IOrganizationRepository, Repositories.OrganizationRepository>();
        services.AddScoped<IUserOrganizationRepository, Repositories.UserOrganizationRepository>();
        // services.AddScoped<ITranscriptionRepository, Repositories.TranscriptionRepository>();
        // services.AddScoped<ITranscriptionSessionRepository, Repositories.TranscriptionSessionRepository>();
        // services.AddScoped<ISubscriptionRepository, Repositories.SubscriptionRepository>();

        // Service registrations
        services.AddScoped<IEmailService, EmailService>();
        // services.AddScoped<Infrastructure.Services.StripeService>();

        return services;
    }
}
