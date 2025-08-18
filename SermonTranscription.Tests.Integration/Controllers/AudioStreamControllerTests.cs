using FluentAssertions;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Tests.Integration.Common;
using System.Net;
using Xunit;

namespace SermonTranscription.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for AudioStreamController
/// </summary>
public class AudioStreamControllerTests : BaseIntegrationTest
{
    public AudioStreamControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    #region Start Audio Stream Tests

    [Fact]
    public async Task StartAudioStream_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var session = await CreateTranscriptionSessionAsync(organization.Id, user.Id);

        var request = new StartAudioStreamRequest
        {
            SessionId = session.Id,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1
        };

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{session.Id}/start", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadApiResponseAsync<AudioStreamStatusResponse>(response);

        result.Should().NotBeNull();
        result.SessionId.Should().Be(session.Id);
        result.IsActive.Should().BeTrue();
        result.Status.Should().Be("InProgress");
        result.CanReceiveAudio.Should().BeTrue();
    }

    [Fact]
    public async Task StartAudioStream_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearHeaders(); // Ensure no authentication

        var request = new StartAudioStreamRequest
        {
            SessionId = Guid.NewGuid(),
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1
        };

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{request.SessionId}/start", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StartAudioStream_WithInvalidSession_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();

        var request = new StartAudioStreamRequest
        {
            SessionId = Guid.NewGuid(), // Non-existent session
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1
        };

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{request.SessionId}/start", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartAudioStream_WithInvalidAudioFormat_ShouldReturnBadRequest()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var session = await CreateTranscriptionSessionAsync(organization.Id, user.Id);

        var request = new StartAudioStreamRequest
        {
            SessionId = session.Id,
            AudioFormat = "invalid-format",
            SampleRate = 16000,
            Channels = 1
        };

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{session.Id}/start", CreateJsonContent(request));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Stop Audio Stream Tests

    [Fact]
    public async Task StopAudioStream_WithValidSession_ShouldReturnSuccess()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var session = await CreateTranscriptionSessionAsync(organization.Id, user.Id);

        // First start the stream
        await StartAudioStreamAsync(session.Id);

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{session.Id}/stop", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadApiResponseAsync<AudioStreamStatusResponse>(response);

        result.Should().NotBeNull();
        result.SessionId.Should().Be(session.Id);
        result.IsActive.Should().BeFalse();
        result.Status.Should().Be("stopped");
    }

    [Fact]
    public async Task StopAudioStream_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearHeaders();
        var sessionId = Guid.NewGuid();

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{sessionId}/stop", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StopAudioStream_WithInvalidSession_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var sessionId = Guid.NewGuid(); // Non-existent session

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{sessionId}/stop", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Process Audio Chunk Tests

    [Fact]
    public async Task StreamAudio_WithValidChunk_ShouldReturnSuccess()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var session = await CreateTranscriptionSessionAsync(organization.Id, user.Id);

        // Start the stream first
        await StartAudioStreamAsync(session.Id);

        var audioData = new byte[] { 0x52, 0x49, 0x46, 0x46 }; // Simple WAV header

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{session.Id}/stream?chunkIndex=0&isFinalChunk=false",
            new ByteArrayContent(audioData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadSuccessResponseAsync(response);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("processed successfully");
    }

    [Fact]
    public async Task StreamAudio_WithEmptyData_ShouldReturnBadRequest()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var session = await CreateTranscriptionSessionAsync(organization.Id, user.Id);

        var audioData = Array.Empty<byte>(); // Empty audio data

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{session.Id}/stream?chunkIndex=0&isFinalChunk=false",
            new ByteArrayContent(audioData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StreamAudio_WithoutActiveStream_ShouldReturnBadRequest()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var session = await CreateTranscriptionSessionAsync(organization.Id, user.Id);

        // Don't start the stream - send chunk directly
        var audioData = new byte[] { 0x52, 0x49, 0x46, 0x46 };

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{session.Id}/stream?chunkIndex=0&isFinalChunk=false",
            new ByteArrayContent(audioData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StreamAudio_WithLargeChunk_ShouldReturnBadRequest()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var session = await CreateTranscriptionSessionAsync(organization.Id, user.Id);

        await StartAudioStreamAsync(session.Id);

        var largeAudioData = new byte[15 * 1024 * 1024]; // 15MB - exceeds typical limit

        // Act
        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{session.Id}/stream?chunkIndex=0&isFinalChunk=false",
            new ByteArrayContent(largeAudioData));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Get Stream Status Tests

    [Fact]
    public async Task GetAudioStreamStatus_WithValidSession_ShouldReturnStatus()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var session = await CreateTranscriptionSessionAsync(organization.Id, user.Id);

        await StartAudioStreamAsync(session.Id);

        // Act
        var response = await HttpClient.GetAsync($"/api/v1.0/audio/{session.Id}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadApiResponseAsync<AudioStreamStatusResponse>(response);

        result.Should().NotBeNull();
        result.SessionId.Should().Be(session.Id);
        result.IsActive.Should().BeTrue();
        result.Status.Should().Be("InProgress");
        result.CanReceiveAudio.Should().BeTrue();
        result.SupportsWebSocket.Should().BeTrue();
        result.SupportsChunkedUpload.Should().BeTrue();
        result.MaxChunkSizeBytes.Should().BeGreaterThan(0);
        result.SupportedAudioFormats.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAudioStreamStatus_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearHeaders();
        var sessionId = Guid.NewGuid();

        // Act
        var response = await HttpClient.GetAsync($"/api/v1.0/audio/{sessionId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAudioStreamStatus_WithInvalidSession_ShouldReturnNotFound()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();
        var sessionId = Guid.NewGuid(); // Non-existent session

        // Act
        var response = await HttpClient.GetAsync($"/api/v1.0/audio/{sessionId}/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Get Configuration Tests

    [Fact]
    public async Task GetAudioStreamConfiguration_WithValidOrganization_ShouldReturnConfiguration()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/audio/configuration");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await ReadApiResponseAsync<AudioStreamConfiguration>(response);

        result.Should().NotBeNull();
        result.MaxChunkSizeBytes.Should().BeGreaterThan(0);
        result.MaxSessionDuration.Should().BeGreaterThan(TimeSpan.Zero);
        result.SupportedFormats.Should().NotBeEmpty();
        result.SupportedSampleRates.Should().NotBeEmpty();
        result.EnableRealTimeTranscription.Should().BeTrue();
        result.EnableAudioBuffering.Should().BeTrue();
    }

    [Fact]
    public async Task GetAudioStreamConfiguration_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearHeaders();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/audio/configuration");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAudioStreamConfiguration_WithoutOrganizationHeader_ShouldReturnForbidden()
    {
        // Arrange
        var (user, _, _) = await CreateTestUserWithOrganizationAsync();
        var token = GenerateJwtTokenAsync(user);

        SetAuthorizationHeader(token);
        // Don't add X-Organization-Id header

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/audio/configuration");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Content Type and Response Format Tests

    [Fact]
    public async Task AudioStreamEndpoints_ShouldReturnCorrectContentType()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/audio/configuration");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task AudioStreamEndpoints_ShouldReturnConsistentApiResponseFormat()
    {
        // Arrange
        var (user, organization, token) = await CreateAuthenticatedUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/api/v1.0/audio/configuration");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeEmpty();

        // Should contain standard API response properties
        responseContent.Should().Contain("\"success\":");
        responseContent.Should().Contain("\"data\":");
    }

    #endregion

    #region Helper Methods

    private async Task<TranscriptionSession> CreateTranscriptionSessionAsync(Guid organizationId, Guid userId)
    {
        var session = new TranscriptionSession
        {
            Id = Guid.NewGuid(),
            Title = "Test Session",
            OrganizationId = organizationId,
            CreatedByUserId = userId, // Fix: Set the required foreign key
            Status = SessionStatus.InProgress, // Changed to InProgress so audio streaming can work
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.TranscriptionSessions.Add(session);
        await DbContext.SaveChangesAsync();

        return session;
    }

    private async Task StartAudioStreamAsync(Guid sessionId)
    {
        var request = new StartAudioStreamRequest
        {
            SessionId = sessionId,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1
        };

        var response = await HttpClient.PostAsync($"/api/v1.0/audio/{sessionId}/start", CreateJsonContent(request));
        response.EnsureSuccessStatusCode();
    }

    #endregion
}
