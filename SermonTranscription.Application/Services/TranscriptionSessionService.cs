using AutoMapper;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Application.Common;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Domain.Common;

namespace SermonTranscription.Application.Services;

public class TranscriptionSessionService : ITranscriptionSessionService
{
    private readonly ITranscriptionSessionRepository _sessionRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public TranscriptionSessionService(
        ITranscriptionSessionRepository sessionRepository,
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _sessionRepository = sessionRepository;
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<TranscriptionSessionResponse>> CreateSessionAsync(
        CreateTranscriptionSessionRequest request,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate organization exists
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
                return ServiceResult<TranscriptionSessionResponse>.Failure("Organization not found");

            // Validate user exists and belongs to organization
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                return ServiceResult<TranscriptionSessionResponse>.Failure("User not found");

            // Create session entity
            var session = new TranscriptionSession
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Description = request.Description,
                Language = request.Language,
                EnableSpeakerDiarization = request.EnableSpeakerDiarization,
                EnablePunctuation = request.EnablePunctuation,
                EnableTimestamps = request.EnableTimestamps,
                AudioStreamUrl = request.AudioStreamUrl,
                AudioFileName = request.AudioFileName,
                IsLive = request.IsLive,
                OrganizationId = organizationId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                Status = SessionStatus.Created
            };

            // Save to database
            await _sessionRepository.AddAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);

            // Map to response
            var response = await MapToResponseAsync(session, cancellationToken);
            return ServiceResult<TranscriptionSessionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure($"Failed to create session: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TranscriptionSessionResponse>> GetSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, organizationId, cancellationToken);
            if (session == null)
                return ServiceResult<TranscriptionSessionResponse>.Failure("Session not found");

            var response = await MapToResponseAsync(session, cancellationToken);
            return ServiceResult<TranscriptionSessionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure($"Failed to get session: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TranscriptionSessionResponse>> UpdateSessionAsync(
        Guid sessionId,
        UpdateTranscriptionSessionRequest request,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, organizationId, cancellationToken);
            if (session == null)
                return ServiceResult<TranscriptionSessionResponse>.Failure("Session not found");

            // Only allow updates if session is not in progress
            if (session.Status == SessionStatus.InProgress)
                return ServiceResult<TranscriptionSessionResponse>.Failure("Cannot update session while in progress");

            // Update fields
            if (request.Title != null)
                session.Title = request.Title;
            if (request.Description != null)
                session.Description = request.Description;
            if (request.Language != null)
                session.Language = request.Language;
            if (request.EnableSpeakerDiarization.HasValue)
                session.EnableSpeakerDiarization = request.EnableSpeakerDiarization.Value;
            if (request.EnablePunctuation.HasValue)
                session.EnablePunctuation = request.EnablePunctuation.Value;
            if (request.EnableTimestamps.HasValue)
                session.EnableTimestamps = request.EnableTimestamps.Value;
            if (request.AudioStreamUrl != null)
                session.AudioStreamUrl = request.AudioStreamUrl;
            if (request.AudioFileName != null)
                session.AudioFileName = request.AudioFileName;

            session.UpdatedAt = DateTime.UtcNow;

            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);

            var response = await MapToResponseAsync(session, cancellationToken);
            return ServiceResult<TranscriptionSessionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure($"Failed to update session: {ex.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, organizationId, cancellationToken);
            if (session == null)
                return ServiceResult<bool>.Failure("Session not found");

            // Only allow deletion if session is not in progress
            if (session.Status == SessionStatus.InProgress)
                return ServiceResult<bool>.Failure("Cannot delete session while in progress");

            await _sessionRepository.DeleteAsync(sessionId, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return ServiceResult<bool>.Failure($"Failed to delete session: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TranscriptionSessionResponse>> StartSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, organizationId, cancellationToken);
            if (session == null)
                return ServiceResult<TranscriptionSessionResponse>.Failure("Session not found");

            session.Start();
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);

            var response = await MapToResponseAsync(session, cancellationToken);
            return ServiceResult<TranscriptionSessionResponse>.Success(response);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure($"Failed to start session: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TranscriptionSessionResponse>> PauseSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, organizationId, cancellationToken);
            if (session == null)
                return ServiceResult<TranscriptionSessionResponse>.Failure("Session not found");

            session.Pause();
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);

            var response = await MapToResponseAsync(session, cancellationToken);
            return ServiceResult<TranscriptionSessionResponse>.Success(response);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure($"Failed to pause session: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TranscriptionSessionResponse>> ResumeSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, organizationId, cancellationToken);
            if (session == null)
                return ServiceResult<TranscriptionSessionResponse>.Failure("Session not found");

            session.Resume();
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);

            var response = await MapToResponseAsync(session, cancellationToken);
            return ServiceResult<TranscriptionSessionResponse>.Success(response);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure($"Failed to resume session: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TranscriptionSessionResponse>> CompleteSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, organizationId, cancellationToken);
            if (session == null)
                return ServiceResult<TranscriptionSessionResponse>.Failure("Session not found");

            session.Complete();
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);

            var response = await MapToResponseAsync(session, cancellationToken);
            return ServiceResult<TranscriptionSessionResponse>.Success(response);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure($"Failed to complete session: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TranscriptionSessionResponse>> CancelSessionAsync(
        Guid sessionId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId, organizationId, cancellationToken);
            if (session == null)
                return ServiceResult<TranscriptionSessionResponse>.Failure("Session not found");

            session.Cancel();
            await _sessionRepository.UpdateAsync(session, cancellationToken);
            await _sessionRepository.SaveChangesAsync(cancellationToken);

            var response = await MapToResponseAsync(session, cancellationToken);
            return ServiceResult<TranscriptionSessionResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return ServiceResult<TranscriptionSessionResponse>.Failure($"Failed to cancel session: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TranscriptionSessionListResponse>> SearchSessionsAsync(
        TranscriptionSessionSearchRequest request,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create pagination request
            var paginationRequest = new PaginationRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending
            };

            var paginatedSessions = await _sessionRepository.SearchSessionsAsync(
                organizationId,
                paginationRequest,
                request.SearchTerm,
                request.Status,
                request.StartDate,
                request.EndDate,
                request.IsLive,
                request.Language,
                request.CreatedByUserId,
                cancellationToken);

            // Map to responses
            var responses = new List<TranscriptionSessionResponse>();
            foreach (var session in paginatedSessions.Items)
            {
                var response = await MapToResponseAsync(session, cancellationToken);
                responses.Add(response);
            }

            var result = new TranscriptionSessionListResponse(
                responses,
                paginatedSessions.TotalCount,
                paginatedSessions.PageNumber,
                paginatedSessions.PageSize);
            return ServiceResult<TranscriptionSessionListResponse>.Success(result);
        }
        catch (Exception ex)
        {
            return ServiceResult<TranscriptionSessionListResponse>.Failure($"Failed to search sessions: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<TranscriptionSessionResponse>>> GetRecentSessionsAsync(
        Guid organizationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _sessionRepository.GetRecentSessionsAsync(organizationId, count, cancellationToken);
            var responses = new List<TranscriptionSessionResponse>();

            foreach (var session in sessions)
            {
                var response = await MapToResponseAsync(session, cancellationToken);
                responses.Add(response);
            }

            return ServiceResult<IEnumerable<TranscriptionSessionResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<TranscriptionSessionResponse>>.Failure($"Failed to get recent sessions: {ex.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<TranscriptionSessionResponse>>> GetActiveSessionsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _sessionRepository.GetActiveSessionsAsync(cancellationToken);
            var organizationSessions = sessions.Where(s => s.OrganizationId == organizationId);
            var responses = new List<TranscriptionSessionResponse>();

            foreach (var session in organizationSessions)
            {
                var response = await MapToResponseAsync(session, cancellationToken);
                responses.Add(response);
            }

            return ServiceResult<IEnumerable<TranscriptionSessionResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            return ServiceResult<IEnumerable<TranscriptionSessionResponse>>.Failure($"Failed to get active sessions: {ex.Message}");
        }
    }

    public async Task<ServiceResult<int>> GetActiveSessionCountAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await _sessionRepository.GetActiveSessionCountAsync(organizationId, cancellationToken);
            return ServiceResult<int>.Success(count);
        }
        catch (Exception ex)
        {
            return ServiceResult<int>.Failure($"Failed to get active session count: {ex.Message}");
        }
    }

    public async Task<ServiceResult<TimeSpan>> GetTotalSessionDurationAsync(
        Guid organizationId,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var duration = await _sessionRepository.GetTotalSessionDurationAsync(organizationId, fromDate, cancellationToken);
            return ServiceResult<TimeSpan>.Success(duration);
        }
        catch (Exception ex)
        {
            return ServiceResult<TimeSpan>.Failure($"Failed to get total session duration: {ex.Message}");
        }
    }

    private async Task<TranscriptionSessionResponse> MapToResponseAsync(
        TranscriptionSession session,
        CancellationToken cancellationToken = default)
    {
        var response = _mapper.Map<TranscriptionSessionResponse>(session);

        // Get organization name
        var organization = await _organizationRepository.GetByIdAsync(session.OrganizationId, cancellationToken);
        response.OrganizationName = organization?.Name ?? "Unknown Organization";

        // Get user name
        var user = await _userRepository.GetByIdAsync(session.CreatedByUserId, cancellationToken);
        response.CreatedByUserName = user?.FullName ?? "Unknown User";

        // Set computed properties
        response.Duration = session.Duration;
        response.IsActive = session.IsActive;
        response.CanStart = session.CanStart;
        response.CanPause = session.CanPause;
        response.CanResume = session.CanResume;
        response.CanComplete = session.CanComplete;

        // Get transcription count
        var sessionWithTranscriptions = await _sessionRepository.GetWithTranscriptionsAsync(session.Id, cancellationToken);
        response.TranscriptionCount = sessionWithTranscriptions?.Transcriptions.Count ?? 0;

        // Calculate total transcription duration
        if (sessionWithTranscriptions?.Transcriptions.Any() == true)
        {
            var totalTicks = sessionWithTranscriptions.Transcriptions
                .Where(t => t.Duration.HasValue)
                .Sum(t => t.Duration.Value.Ticks);
            response.TotalTranscriptionDuration = TimeSpan.FromTicks(totalTicks);
        }

        return response;
    }
}
