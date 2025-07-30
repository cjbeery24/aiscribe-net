using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SermonTranscription.Tests.Integration.Common;
using SermonTranscription.Application.DTOs;
using System.Net;
using System.Text.Json;

namespace SermonTranscription.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for validation and error handling functionality
/// </summary>
public class ValidationAndErrorHandlingTests : BaseIntegrationTest
{
    public ValidationAndErrorHandlingTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Registration Validation Tests

    [Fact]
    public async Task Register_WithEmptyEmail_ShouldReturnValidationError()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "",
            Password = "SecurePassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result.ValidationErrors.Should().Contain(e => e.Field == "Email");
        result.TraceId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "SecurePassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().ContainSingle(e => e.Field == "Email");
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnValidationError()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "weak",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().ContainSingle(e => e.Field == "Password");
    }

    [Fact]
    public async Task Register_WithEmptyFirstName_ShouldReturnValidationError()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePassword123!",
            FirstName = "",
            LastName = "User"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().ContainSingle(e => e.Field == "FirstName");
    }

    [Fact]
    public async Task Register_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "weak",
            FirstName = "",
            LastName = ""
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().HaveCount(4);
        result.ValidationErrors.Should().Contain(e => e.Field == "Email");
        result.ValidationErrors.Should().Contain(e => e.Field == "Password");
        result.ValidationErrors.Should().Contain(e => e.Field == "FirstName");
        result.ValidationErrors.Should().Contain(e => e.Field == "LastName");
    }

    #endregion

    #region Organization Creation Validation Tests

    [Fact]
    public async Task CreateOrganization_WithEmptyName_ShouldReturnValidationError()
    {
        // Arrange
        var (user, _, token) = await CreateAuthenticatedUserAsync();

        var request = new CreateOrganizationRequest
        {
            Name = "",
            Description = "Test organization"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().ContainSingle(e => e.Field == "Name");
    }

    [Fact]
    public async Task CreateOrganization_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var (user, _, token) = await CreateAuthenticatedUserAsync();

        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            ContactEmail = "invalid-email"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().ContainSingle(e => e.Field == "ContactEmail");
    }

    [Fact]
    public async Task CreateOrganization_WithInvalidWebsiteUrl_ShouldReturnValidationError()
    {
        // Arrange
        var (user, _, token) = await CreateAuthenticatedUserAsync();

        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            WebsiteUrl = "not-a-valid-url"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().ContainSingle(e => e.Field == "WebsiteUrl");
    }

    [Fact]
    public async Task CreateOrganization_WithExceededLengths_ShouldReturnValidationError()
    {
        // Arrange
        var (user, _, token) = await CreateAuthenticatedUserAsync();

        var request = new CreateOrganizationRequest
        {
            Name = new string('A', 201), // Exceeds 200 character limit
            Description = new string('B', 1001), // Exceeds 1000 character limit
            ContactEmail = "test@example.com"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().Contain(e => e.Field == "Name");
        result.ValidationErrors.Should().Contain(e => e.Field == "Description");
    }

    #endregion

    #region Login Validation Tests

    [Fact]
    public async Task Login_WithEmptyEmail_ShouldReturnValidationError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "",
            Password = "SecurePassword123!"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/login", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().Contain(e => e.Field == "Email");
    }

    [Fact]
    public async Task Login_WithEmptyPassword_ShouldReturnValidationError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/login", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().ContainSingle(e => e.Field == "Password");
    }

    #endregion

    #region Global Exception Handling Tests

    [Fact]
    public async Task NonExistentEndpoint_ShouldReturnNotFound()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/nonexistent");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidJson_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register",
            new StringContent(invalidJson, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnauthorizedEndpoint_ShouldReturnUnauthorized()
    {
        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/users/profile");

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidOrganizationHeader_ShouldReturnForbidden()
    {
        // Arrange
        var (user, _, token) = await CreateAuthenticatedUserAsync();
        SetOrganizationHeader(Guid.NewGuid()); // Non-existent organization

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/organizations/load");

        // Assert
        // This endpoint requires a valid organization header
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    #endregion

    #region Error Response Format Tests

    [Fact]
    public async Task ValidationErrorResponse_ShouldHaveCorrectStructure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "weak",
            FirstName = "",
            LastName = ""
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result.Errors.Should().NotBeEmpty();
        result.ValidationErrors.Should().NotBeEmpty();
        result.TraceId.Should().NotBeEmpty();

        // Verify validation error structure
        var emailError = result.ValidationErrors.FirstOrDefault(e => e.Field == "Email");
        emailError.Should().NotBeNull();
        emailError!.Field.Should().Be("Email");
        emailError.ErrorCode.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task ErrorResponse_ShouldHaveCorrectStructure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/login", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result.Errors.Should().NotBeEmpty();
    }

    #endregion

    #region Content Type and Headers Tests

    [Fact]
    public async Task ValidationErrorResponse_ShouldHaveCorrectContentType()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task ErrorResponse_ShouldBeValidJson()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

        // Verify it's valid JSON
        Action parseJson = () => JsonSerializer.Deserialize<ValidationErrorResponse>(content);
        parseJson.Should().NotThrow();
    }

    #endregion

    #region Complex Validation Scenarios

    [Fact]
    public async Task UpdateOrganization_WithComplexValidation_ShouldReturnAllErrors()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        var request = new UpdateOrganizationRequest
        {
            Name = "", // Required field empty
            Description = new string('A', 1001), // Too long
            ContactEmail = "invalid-email", // Invalid email
            WebsiteUrl = "not-a-url", // Invalid URL
            PhoneNumber = new string('1', 21), // Too long
            Address = new string('B', 501), // Too long
            City = new string('C', 101), // Too long
            State = new string('D', 51), // Too long
            PostalCode = new string('E', 21), // Too long
            Country = new string('F', 101) // Too long
        };

        // Act
        var response = await HttpClient.PutAsync("/api/v1.0/organizations", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().HaveCountGreaterThan(5);
        result.ValidationErrors.Should().Contain(e => e.Field == "ContactEmail");
        result.ValidationErrors.Should().Contain(e => e.Field == "WebsiteUrl");
        result.ValidationErrors.Should().Contain(e => e.Field == "Description");
    }

    [Fact]
    public async Task InviteUser_WithValidationErrors_ShouldReturnDetailedErrors()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedAdminAsync();

        var request = new InviteUserRequest
        {
            Email = "invalid-email",
            FirstName = "",
            LastName = "",
            Role = "InvalidRole"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/invite", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().Contain(e => e.Field == "Email");
        result.ValidationErrors.Should().Contain(e => e.Field == "FirstName");
        result.ValidationErrors.Should().Contain(e => e.Field == "LastName");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task RequestWithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        var request = new
        {
            Email = (string)null,
            Password = (string)null,
            FirstName = (string)null,
            LastName = (string)null
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        await AssertStatusCodeAsync(response, HttpStatusCode.BadRequest);

        var result = await ReadErrorResponseAsync(response);
        result.Should().NotBeNull();
        result!.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RequestWithSpecialCharacters_ShouldHandleGracefully()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "SecurePassword123!",
            FirstName = "Test<script>alert('xss')</script>",
            LastName = "User<script>alert('xss')</script>"
        };

        // Act
        var response = await HttpClient.PostAsync("/api/v1.0/auth/register", CreateJsonContent(request));

        // Assert
        // Should either succeed (if special chars are allowed) or return validation error
        // The important thing is it doesn't crash
        response.Should().NotBeNull();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    #endregion
}
