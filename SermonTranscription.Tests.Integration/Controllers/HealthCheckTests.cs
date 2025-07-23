using FluentAssertions;
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
        ClearHeaders();

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
        DbContext.Should().NotBeNull();

        // Verify database was created
        var canConnect = await DbContext.Database.CanConnectAsync();
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
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task DatabaseContext_ShouldSupport_EntityOperations()
    {
        // Arrange
        var user = await CreateTestUserAsync("test@example.com");
        var organization = await CreateTestOrganizationAsync("Test Organization");

        // Act & Assert - Verify entities were created
        var retrievedUser = await DbContext.Users.FindAsync(user.Id);
        retrievedUser.Should().NotBeNull();
        retrievedUser!.Email.Should().Be("test@example.com");

        var retrievedOrg = await DbContext.Organizations.FindAsync(organization.Id);
        retrievedOrg.Should().NotBeNull();
        retrievedOrg!.Name.Should().Be("Test Organization");
    }
}
