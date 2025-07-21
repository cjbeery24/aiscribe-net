using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SermonTranscription.Api;
using SermonTranscription.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SermonTranscription.Tests.Integration.Common;

/// <summary>
/// Base class for integration tests providing ASP.NET Core test server
/// </summary>
public abstract class BaseIntegrationTest : IClassFixture<BaseIntegrationTest.TestWebApplicationFactory>, IAsyncLifetime
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

    public Task InitializeAsync()
    {
        try
        {
            DbContext.Database.OpenConnection();
            DbContext.Database.EnsureCreated(); // Use EnsureCreated for schema validation
            Console.WriteLine("Migrations applied successfully.");

            // Debug: List all tables in the database
            var tables = DbContext.Database.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'").ToList();
            Console.WriteLine("Tables in database: " + string.Join(", ", tables));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        DbContext.Database.EnsureDeleted(); // Clean up database
        DbContext.Database.CloseConnection();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Custom WebApplicationFactory for integration tests
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection;

        public TestWebApplicationFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open(); // Keep connection open for in-memory database
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Remove any existing configuration sources
                config.Sources.Clear();
                // Optionally load appsettings.Test.json
                config.AddJsonFile("appsettings.Test.json", optional: true);
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Register AppDbContext with SQLite in-memory
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite(_connection));

                // Reduce logging noise in tests
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });

            builder.UseEnvironment("Test");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection?.Close();
            _connection?.Dispose();
        }
    }

    /// <summary>
    /// Clear all data from the test database
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        // For in-memory database, clear all entities manually
        DbContext.UserOrganizations.RemoveRange(DbContext.UserOrganizations);
        DbContext.Users.RemoveRange(DbContext.Users);
        DbContext.Organizations.RemoveRange(DbContext.Organizations);
        DbContext.Subscriptions.RemoveRange(DbContext.Subscriptions);
        DbContext.TranscriptionSessions.RemoveRange(DbContext.TranscriptionSessions);
        DbContext.Transcriptions.RemoveRange(DbContext.Transcriptions);
        DbContext.TranscriptionSegments.RemoveRange(DbContext.TranscriptionSegments);

        await DbContext.SaveChangesAsync();
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
        try
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
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Create UserOrganization join entity
            var userOrganization = new SermonTranscription.Domain.Entities.UserOrganization
            {
                UserId = user.Id,
                OrganizationId = organization.Id,
                Role = SermonTranscription.Domain.Enums.UserRole.OrganizationUser,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                User = user,
                Organization = organization
            };

            DbContext.Organizations.Add(organization);
            DbContext.Users.Add(user);
            DbContext.UserOrganizations.Add(userOrganization);
            await DbContext.SaveChangesAsync();

            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateTestUserAsync failed: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
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
    protected static async Task AssertSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
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
