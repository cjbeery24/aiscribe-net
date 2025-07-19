using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace SermonTranscription.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database Configuration - conditionally register based on environment
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (environment == "Test" || string.IsNullOrEmpty(connectionString) || connectionString.Contains(":memory:"))
        {
            // Use in-memory database for testing
            services.AddDbContext<Data.AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        }
        else
        {
            // Use PostgreSQL for production/development
            services.AddDbContext<Data.AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        // Redis Configuration - only register if connection string is provided
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(provider =>
            {
                return ConnectionMultiplexer.Connect(redisConnectionString);
            });
        }

        // HTTP Client Configuration
        services.AddHttpClient();

        // External Service Configurations (will be implemented later)
        // services.AddScoped<IGladiaService, GladiaService>();
        // services.AddScoped<IEmailService, EmailService>();
        // services.AddScoped<IStripeService, StripeService>();

        // Repository Registrations (will be implemented later)
        // services.AddScoped<IUserRepository, UserRepository>();
        // services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        // services.AddScoped<ITranscriptionRepository, TranscriptionRepository>();

        return services;
    }
} 