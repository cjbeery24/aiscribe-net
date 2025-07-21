using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SermonTranscription.Tests.Integration.Common;
using System.Net;

namespace SermonTranscription.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for health check endpoint
/// </summary>
public class HealthCheckTests : BaseIntegrationTest
{
    public HealthCheckTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task HealthCheck_ShouldReturn_HealthyStatus()
    {
        // Act
        var response = await HttpClient.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthCheck_ShouldNotRequire_Authentication()
    {
        // Arrange - Ensure no auth header is set
        ClearAuthorizationHeader();

        // Act
        var response = await HttpClient.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturn_ExpectedContentType()
    {
        // Act
        var response = await HttpClient.GetAsync("/health");

        // Assert
        await AssertSuccessStatusCodeAsync(response);
        response.Content.Headers.ContentType?.ToString().Should().Be("text/plain");
    }

    [Fact]
    public async Task Application_ShouldStart_WithTestConfiguration()
    {
        // This test validates that our test application factory
        // can successfully start the application with test configuration

        // Act - Make any request to ensure the app is running
        var response = await HttpClient.GetAsync("/health");

        // Assert
        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();

        // Verify we can access the database context
        var dbContext = GetDbContext();
        dbContext.Should().NotBeNull();

        // Verify database was created
        var canConnect = await dbContext.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task TestInfrastructure_ShouldProvide_CleanDatabaseForEachTest()
    {
        // Arrange - Create some test data
        var testUser = await CreateTestUserAsync("test1@example.com");
        testUser.Should().NotBeNull();

        // Verify data exists
        var userCount = DbContext.Users.Count();
        userCount.Should().BeGreaterThan(0);

        // Act - Clear database
        await ClearDatabaseAsync();

        // Assert - Database should be empty
        var userCountAfterClear = DbContext.Users.Count();
        userCountAfterClear.Should().Be(0);

        var orgCountAfterClear = DbContext.Organizations.Count();
        orgCountAfterClear.Should().Be(0);
    }

    [Fact]
    public async Task TestInfrastructure_ShouldSupport_HttpClientHelpers()
    {
        // Arrange
        var testData = new { message = "test", value = 123 };

        // Act - Test JSON content creation
        var jsonContent = CreateJsonContent(testData);

        // Assert
        jsonContent.Should().NotBeNull();
        jsonContent.Headers.ContentType?.MediaType.Should().Be("application/json");

        var contentString = await jsonContent.ReadAsStringAsync();
        contentString.Should().Contain("test");
        contentString.Should().Contain("123");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/Health")]
    [InlineData("/HEALTH")]
    public async Task HealthCheck_ShouldBe_CaseInsensitive(string endpoint)
    {
        // Act
        var response = await HttpClient.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DatabaseContext_ShouldSupport_EntityOperations()
    {
        // Arrange
        var user = await CreateTestUserAsync("dbtest@example.com", "DB", "Test");

        // Act & Assert - Verify user was created
        user.Should().NotBeNull();
        user.Email.Should().Be("dbtest@example.com");
        user.FirstName.Should().Be("DB");
        user.LastName.Should().Be("Test");

        // Verify in database
        var savedUser = await DbContext.Users.FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(user.Email);

        // Verify organization was also created
        var userOrg = await DbContext.UserOrganizations
            .Include(uo => uo.Organization)
            .FirstOrDefaultAsync(uo => uo.UserId == user.Id);
        userOrg.Should().NotBeNull();
        userOrg!.Organization.Should().NotBeNull();
        userOrg.Organization.Name.Should().Be("Test Organization");
    }
}
