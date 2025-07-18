using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SermonTranscription.Api;
using SermonTranscription.Infrastructure.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SermonTranscription.Tests.Integration.Common;

/// <summary>
/// Base class for integration tests providing ASP.NET Core test server
/// </summary>
public abstract class BaseIntegrationTest : IClassFixture<BaseIntegrationTest.TestWebApplicationFactory>
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient HttpClient;
    protected readonly AppDbContext DbContext;

    protected BaseIntegrationTest(TestWebApplicationFactory factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
        DbContext = factory.Services.GetRequiredService<AppDbContext>();
    }

    /// <summary>
    /// Custom WebApplicationFactory for integration tests
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Remove any existing configuration sources
                config.Sources.Clear();
                
                // Add test-specific configuration
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                    ["JwtSettings:SecretKey"] = "test-secret-key-for-integration-tests-must-be-long-enough",
                    ["JwtSettings:Issuer"] = "TestIssuer",
                    ["JwtSettings:Audience"] = "TestAudience",
                    ["JwtSettings:ExpirationInMinutes"] = "60",
                    ["Serilog:MinimumLevel:Default"] = "Warning",
                    ["AllowedOrigins:0"] = "http://localhost:3000"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestDb_" + Guid.NewGuid());
                    options.EnableSensitiveDataLogging();
                });

                // Reduce logging noise in tests
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });

                // Build service provider and ensure database is created
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureCreated();
            });

            builder.UseEnvironment("Testing");
        }
    }

    /// <summary>
    /// Clear all data from the test database
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Add authorization header to HTTP client
    /// </summary>
    protected void SetAuthorizationHeader(string token)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Remove authorization header from HTTP client
    /// </summary>
    protected void ClearAuthorizationHeader()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Serialize object to JSON for HTTP requests
    /// </summary>
    protected static StringContent CreateJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Deserialize JSON response to object
    /// </summary>
    protected static async Task<T?> ReadJsonResponseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Create a test user in the database and return it
    /// </summary>
    protected async Task<SermonTranscription.Domain.Entities.User> CreateTestUserAsync(
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User",
        string role = "OrganizationUser")
    {
        var organization = new SermonTranscription.Domain.Entities.Organization
        {
            Id = Guid.NewGuid(),
            Name = "Test Organization",
            Slug = "test-org",
            ContactEmail = "admin@test.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            MaxUsers = 10,
            MaxTranscriptionHours = 100,
            CanExportTranscriptions = true,
            HasRealtimeTranscription = true
        };

        var user = new SermonTranscription.Domain.Entities.User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = "$2a$11$test.hash.for.integration.tests", // BCrypt hash
            IsEmailVerified = true,

            OrganizationId = organization.Id,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        DbContext.Organizations.Add(organization);
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Generate a test JWT token for authentication
    /// </summary>
    protected string GenerateTestJwtToken(SermonTranscription.Domain.Entities.User user, string role = "OrganizationUser")
    {
        // This would typically use the same JWT service as the application
        // For now, returning a placeholder that matches the test configuration
        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["role"] = role,
            ["organizationId"] = user.OrganizationId.ToString(),
            ["iss"] = "TestIssuer",
            ["aud"] = "TestAudience",
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        // In a real implementation, you'd use the JWT service to create the token
        // For integration tests, you might want to use a test-specific token generation
        return "test-jwt-token-placeholder";
    }

    /// <summary>
    /// Seed the database with test data
    /// </summary>
    protected async Task SeedDatabaseAsync()
    {
        // Add any common test data here
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Assert that the response is successful (2xx status code)
    /// </summary>
    protected static void AssertSuccessStatusCode(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = response.Content.ReadAsStringAsync().Result;
            throw new HttpRequestException(
                $"Expected successful status code but got {response.StatusCode}. Content: {content}");
        }
    }

    /// <summary>
    /// Get the current database context (useful for verification in tests)
    /// </summary>
    protected AppDbContext GetDbContext()
    {
        return Factory.Services.GetRequiredService<AppDbContext>();
    }
} 