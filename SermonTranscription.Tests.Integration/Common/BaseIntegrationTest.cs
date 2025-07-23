using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Infrastructure.Services;
using SermonTranscription.Application.Services;
using Moq;
using StackExchange.Redis;

namespace SermonTranscription.Tests.Integration.Common;

/// <summary>
/// Base class for integration tests providing ASP.NET Core test server with multi-tenant support
/// </summary>
public abstract class BaseIntegrationTest : IClassFixture<BaseIntegrationTest.TestWebApplicationFactory>, IAsyncLifetime
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient HttpClient;
    protected readonly AppDbContext DbContext;
    protected readonly IJwtService JwtService;
    protected readonly IUserService UserService;
    protected readonly IOrganizationService OrganizationService;

    protected BaseIntegrationTest(TestWebApplicationFactory factory)
    {
        Factory = factory;
        HttpClient = factory.CreateClient();
        DbContext = factory.Services.GetRequiredService<AppDbContext>();
        JwtService = factory.Services.GetRequiredService<IJwtService>();
        UserService = factory.Services.GetRequiredService<IUserService>();
        OrganizationService = factory.Services.GetRequiredService<IOrganizationService>();
    }

    public async Task InitializeAsync()
    {
        // Ensure database is created and schema is applied
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();

        // Verify schema creation
        if (!await DbContext.Database.CanConnectAsync())
        {
            throw new InvalidOperationException("Failed to connect to the test database.");
        }

        // Clear any existing data
        await ClearDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up database
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.CloseConnectionAsync();
        // Don't manually dispose DbContext - let DI container handle it
    }

    /// <summary>
    /// Custom WebApplicationFactory for integration tests
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection; // Keep connection alive

        public TestWebApplicationFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:;Cache=Shared");
            _connection.Open(); // Open connection explicitly
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.Sources.Clear();
                config.AddJsonFile("appsettings.Test.json", optional: false);
                config.SetBasePath(Directory.GetCurrentDirectory());
            });

            builder.ConfigureServices((context, services) =>
            {
                // Remove existing DbContext configuration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Register DbContext with hardcoded SQLite connection
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }, ServiceLifetime.Singleton); // Singleton for test isolation

                // Reduce logging noise
                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });

                // Mock IEmailService to avoid external calls
                services.AddScoped<IEmailService, MockEmailService>();

                // Mock Redis to avoid external dependency
                services.AddSingleton<IConnectionMultiplexer>(sp => Mock.Of<IConnectionMultiplexer>());
            });

            builder.UseEnvironment("Test");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Mock EmailService for testing
    public class MockEmailService : IEmailService
    {
        public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

        public Task SendEmailAsync(string to, string subject, string body)
        {
            SentEmails.Add((to, subject, body));
            return Task.CompletedTask;
        }

        public Task<bool> SendInvitationEmailAsync(string to, string firstName, string lastName, string organizationName, string invitationUrl, string? inviterName = null)
        {
            SentEmails.Add((to, "Invitation", $"Invitation to join {organizationName}"));
            return Task.FromResult(true);
        }

        public Task<bool> SendPasswordResetEmailAsync(string to, string firstName, string resetUrl)
        {
            SentEmails.Add((to, "Password Reset", "Password reset email"));
            return Task.FromResult(true);
        }

        public Task<bool> SendWelcomeEmailAsync(string to, string firstName, string organizationName)
        {
            SentEmails.Add((to, "Welcome", $"Welcome to {organizationName}"));
            return Task.FromResult(true);
        }
    }

    #region Database Helpers

    protected async Task ClearDatabaseAsync()
    {
        try
        {
            if (await DbContext.Database.CanConnectAsync())
            {
                // Only clear tables if they exist
                if (DbContext.Users.Any())
                    DbContext.Users.RemoveRange(DbContext.Users);
                if (DbContext.UserOrganizations.Any())
                    DbContext.UserOrganizations.RemoveRange(DbContext.UserOrganizations);
                if (DbContext.Organizations.Any())
                    DbContext.Organizations.RemoveRange(DbContext.Organizations);
                if (DbContext.Subscriptions.Any())
                    DbContext.Subscriptions.RemoveRange(DbContext.Subscriptions);
                if (DbContext.RefreshTokens.Any())
                    DbContext.RefreshTokens.RemoveRange(DbContext.RefreshTokens);

                await DbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not clear database: {ex.Message}");
        }
    }

    protected async Task<User> CreateTestUserAsync(
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User",
        string password = "TestPassword123!")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        // Hash password
        var passwordHasher = new PasswordHasher();
        user.PasswordHash = passwordHasher.HashPassword(password);

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    protected async Task<Organization> CreateTestOrganizationAsync(
        string name = "Test Organization",
        string description = "Test organization for integration tests")
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        organization.UpdateSlug();
        DbContext.Organizations.Add(organization);
        await DbContext.SaveChangesAsync();
        return organization;
    }

    protected async Task<UserOrganization> CreateTestUserOrganizationAsync(
        User user,
        Organization organization,
        UserRole role = UserRole.OrganizationUser)
    {
        var userOrg = new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = organization.Id,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.UserOrganizations.Add(userOrg);
        await DbContext.SaveChangesAsync();
        return userOrg;
    }

    protected async Task<(User User, Organization Organization, UserOrganization UserOrg)> CreateTestUserWithOrganizationAsync(
        string email = "test@example.com",
        UserRole role = UserRole.OrganizationUser)
    {
        var user = await CreateTestUserAsync(email);
        var organization = await CreateTestOrganizationAsync();
        var userOrg = await CreateTestUserOrganizationAsync(user, organization, role);

        return (user, organization, userOrg);
    }

    #endregion

    #region Authentication Helpers

    protected string GenerateJwtTokenAsync(User user, Organization? organization = null)
    {
        return JwtService.GenerateAccessToken(user);
    }

    protected void SetAuthorizationHeader(string token)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetOrganizationHeader(Guid organizationId)
    {
        HttpClient.DefaultRequestHeaders.Remove("X-Organization-ID");
        HttpClient.DefaultRequestHeaders.Add("X-Organization-ID", organizationId.ToString());
    }

    protected void ClearHeaders()
    {
        HttpClient.DefaultRequestHeaders.Authorization = null;
        HttpClient.DefaultRequestHeaders.Remove("X-Organization-ID");
    }

    #endregion

    #region HTTP Helpers

    protected static StringContent CreateJsonContent(object obj)
    {
        var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    protected static async Task<T?> ReadJsonResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(content))
            return default;

        return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    protected static async Task AssertSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {response.StatusCode}: {content}");
        }
    }

    protected static async Task AssertStatusCodeAsync(HttpResponseMessage response, System.Net.HttpStatusCode expectedStatusCode)
    {
        if (response.StatusCode != expectedStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected status {expectedStatusCode} but got {response.StatusCode}: {content}");
        }
    }

    #endregion

    #region Test Data Helpers

    protected async Task<(User User, Organization Organization, string Token)> CreateAuthenticatedUserAsync(
        string email = "test@example.com",
        UserRole role = UserRole.OrganizationUser)
    {
        var (user, organization, _) = await CreateTestUserWithOrganizationAsync(email, role);
        var token = GenerateJwtTokenAsync(user, organization);

        SetAuthorizationHeader(token);
        SetOrganizationHeader(organization.Id);

        return (user, organization, token);
    }

    protected async Task<(User User, Organization Organization, string Token)> CreateAuthenticatedAdminAsync(
        string email = "admin@example.com")
    {
        return await CreateAuthenticatedUserAsync(email, UserRole.OrganizationAdmin);
    }

    #endregion
}
