using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Application.Services;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Tests.Unit.Common;
using Xunit;

namespace SermonTranscription.Tests.Unit.Services;

public class AudioStreamServiceTests : BaseUnitTest
{
    private readonly Mock<ITranscriptionSessionService> _mockSessionService;
    private readonly Mock<IOrganizationRepository> _mockOrganizationRepository;
    private readonly Mock<ILogger<AudioStreamService>> _mockLogger;
    private readonly AudioStreamService _audioStreamService;

    public AudioStreamServiceTests()
    {
        _mockSessionService = new Mock<ITranscriptionSessionService>();
        _mockOrganizationRepository = new Mock<IOrganizationRepository>();
        _mockLogger = new Mock<ILogger<AudioStreamService>>();

        _audioStreamService = new AudioStreamService(
            _mockSessionService.Object,
            _mockOrganizationRepository.Object,
            _mockLogger.Object,
            new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public async Task StartAudioStreamAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new StartAudioStreamRequest
        {
            SessionId = sessionId,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1,
            UseWebSocket = true
        };

        var sessionResponse = new TranscriptionSessionResponse
        {
            Id = sessionId,
            Status = SessionStatus.InProgress,
            OrganizationId = organizationId
        };

        _mockSessionService
            .Setup(x => x.GetSessionAsync(sessionId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<TranscriptionSessionResponse>.Success(sessionResponse));

        // Act
        var result = await _audioStreamService.StartAudioStreamAsync(request, organizationId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(sessionId, result.Data.SessionId);
        Assert.True(result.Data.IsActive);
        Assert.True(result.Data.CanReceiveAudio);
    }

    [Fact]
    public async Task StartAudioStreamAsync_WithInactiveSession_ShouldReturnFailure()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new StartAudioStreamRequest
        {
            SessionId = sessionId,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1,
            UseWebSocket = true
        };

        var sessionResponse = new TranscriptionSessionResponse
        {
            Id = sessionId,
            Status = SessionStatus.Created, // Not active
            OrganizationId = organizationId
        };

        _mockSessionService
            .Setup(x => x.GetSessionAsync(sessionId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<TranscriptionSessionResponse>.Success(sessionResponse));

        // Act
        var result = await _audioStreamService.StartAudioStreamAsync(request, organizationId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not active", result.Message);
    }

    [Fact]
    public async Task StartAudioStreamAsync_WithNonExistentSession_ShouldReturnFailure()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new StartAudioStreamRequest
        {
            SessionId = sessionId,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1,
            UseWebSocket = true
        };

        _mockSessionService
            .Setup(x => x.GetSessionAsync(sessionId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<TranscriptionSessionResponse>.Failure("Session not found"));

        // Act
        var result = await _audioStreamService.StartAudioStreamAsync(request, organizationId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public async Task ProcessAudioChunkAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var audioData = new byte[1024]; // 1KB test data
        var chunkIndex = 0;
        var isFinalChunk = false;

        var sessionResponse = new TranscriptionSessionResponse
        {
            Id = sessionId,
            Status = SessionStatus.InProgress,
            OrganizationId = organizationId
        };

        _mockSessionService
            .Setup(x => x.GetSessionAsync(sessionId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<TranscriptionSessionResponse>.Success(sessionResponse));

        // First start the stream
        var startRequest = new StartAudioStreamRequest
        {
            SessionId = sessionId,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1,
            UseWebSocket = true
        };

        await _audioStreamService.StartAudioStreamAsync(startRequest, organizationId, Guid.NewGuid());

        // Act
        var result = await _audioStreamService.ProcessAudioChunkAsync(
            sessionId, audioData, chunkIndex, isFinalChunk, organizationId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(chunkIndex, result.Data.ChunkIndex);
        Assert.True(result.Data.Success);
        Assert.Equal(audioData.Length, result.Data.SizeBytes);
    }

    [Fact]
    public async Task ProcessAudioChunkAsync_WithEmptyData_ShouldReturnFailure()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var audioData = new byte[0]; // Empty data
        var chunkIndex = 0;
        var isFinalChunk = false;

        var sessionResponse = new TranscriptionSessionResponse
        {
            Id = sessionId,
            Status = SessionStatus.InProgress,
            OrganizationId = organizationId
        };

        _mockSessionService
            .Setup(x => x.GetSessionAsync(sessionId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<TranscriptionSessionResponse>.Success(sessionResponse));

        // First start the stream
        var startRequest = new StartAudioStreamRequest
        {
            SessionId = sessionId,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1,
            UseWebSocket = true
        };

        await _audioStreamService.StartAudioStreamAsync(startRequest, organizationId, Guid.NewGuid());

        // Act
        var result = await _audioStreamService.ProcessAudioChunkAsync(
            sessionId, audioData, chunkIndex, isFinalChunk, organizationId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("No audio data provided", result.Message);
    }

    [Fact]
    public async Task ProcessAudioChunkAsync_WithLargeData_ShouldReturnFailure()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var audioData = new byte[11 * 1024 * 1024]; // 11MB (exceeds 10MB limit)
        var chunkIndex = 0;
        var isFinalChunk = false;

        var sessionResponse = new TranscriptionSessionResponse
        {
            Id = sessionId,
            Status = SessionStatus.InProgress,
            OrganizationId = organizationId
        };

        _mockSessionService
            .Setup(x => x.GetSessionAsync(sessionId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<TranscriptionSessionResponse>.Success(sessionResponse));

        // First start the stream
        var startRequest = new StartAudioStreamRequest
        {
            SessionId = sessionId,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1,
            UseWebSocket = true
        };

        await _audioStreamService.StartAudioStreamAsync(startRequest, organizationId, Guid.NewGuid());

        // Act
        var result = await _audioStreamService.ProcessAudioChunkAsync(
            sessionId, audioData, chunkIndex, isFinalChunk, organizationId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("too large", result.Message);
    }

    [Fact]
    public async Task GetAudioStreamStatusAsync_WithActiveStream_ShouldReturnStatus()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var sessionResponse = new TranscriptionSessionResponse
        {
            Id = sessionId,
            Status = SessionStatus.InProgress,
            OrganizationId = organizationId
        };

        _mockSessionService
            .Setup(x => x.GetSessionAsync(sessionId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<TranscriptionSessionResponse>.Success(sessionResponse));

        // Start the stream first
        var startRequest = new StartAudioStreamRequest
        {
            SessionId = sessionId,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1,
            UseWebSocket = true
        };

        await _audioStreamService.StartAudioStreamAsync(startRequest, organizationId, Guid.NewGuid());

        // Act
        var result = await _audioStreamService.GetAudioStreamStatusAsync(sessionId, organizationId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(sessionId, result.Data.SessionId);
        Assert.True(result.Data.IsActive);
        Assert.True(result.Data.CanReceiveAudio);
    }

    [Fact]
    public async Task StopAudioStreamAsync_WithActiveStream_ShouldReturnSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var sessionResponse = new TranscriptionSessionResponse
        {
            Id = sessionId,
            Status = SessionStatus.InProgress,
            OrganizationId = organizationId
        };

        _mockSessionService
            .Setup(x => x.GetSessionAsync(sessionId, organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<TranscriptionSessionResponse>.Success(sessionResponse));

        // Start the stream first
        var startRequest = new StartAudioStreamRequest
        {
            SessionId = sessionId,
            AudioFormat = "wav",
            SampleRate = 16000,
            Channels = 1,
            UseWebSocket = true
        };

        await _audioStreamService.StartAudioStreamAsync(startRequest, organizationId, Guid.NewGuid());

        // Act
        var result = await _audioStreamService.StopAudioStreamAsync(sessionId, organizationId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task GetAudioStreamConfigurationAsync_WithValidOrganization_ShouldReturnConfiguration()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = new Organization { Id = organizationId, Name = "Test Org" };

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        // Act
        var result = await _audioStreamService.GetAudioStreamConfigurationAsync(organizationId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(10 * 1024 * 1024, result.Data.MaxChunkSizeBytes); // 10MB
        Assert.Equal(TimeSpan.FromHours(4), result.Data.MaxSessionDuration);
        Assert.Contains("wav", result.Data.SupportedFormats);
        Assert.Contains("mp3", result.Data.SupportedFormats);
        Assert.Contains("m4a", result.Data.SupportedFormats);
        Assert.Contains("flac", result.Data.SupportedFormats);
        Assert.True(result.Data.EnableRealTimeTranscription);
        Assert.True(result.Data.EnableAudioBuffering);
        Assert.Equal(5, result.Data.BufferSizeSeconds);
    }

    [Fact]
    public async Task GetAudioStreamConfigurationAsync_WithNonExistentOrganization_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _audioStreamService.GetAudioStreamConfigurationAsync(organizationId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Message);
    }
}
