using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace SermonTranscription.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database Configuration
        services.AddDbContext<Data.AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Redis Configuration
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var configuration = provider.GetService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Redis");
            return ConnectionMultiplexer.Connect(connectionString);
        });

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