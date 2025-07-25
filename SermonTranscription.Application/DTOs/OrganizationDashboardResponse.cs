namespace SermonTranscription.Application.DTOs;

/// <summary>
/// Comprehensive dashboard data for an organization
/// </summary>
public class OrganizationDashboardResponse
{
    /// <summary>
    /// Organization overview statistics
    /// </summary>
    public OrganizationOverviewDto Overview { get; set; } = new();

    /// <summary>
    /// User activity and statistics
    /// </summary>
    public UserActivityDto UserActivity { get; set; } = new();

    /// <summary>
    /// Subscription status and usage information
    /// </summary>
    public SubscriptionStatusDto SubscriptionStatus { get; set; } = new();

    /// <summary>
    /// Transcription statistics and metrics
    /// </summary>
    public TranscriptionStatsDto TranscriptionStats { get; set; } = new();

    /// <summary>
    /// Recent activity and events
    /// </summary>
    public RecentActivityDto RecentActivity { get; set; } = new();
}

/// <summary>
/// Organization overview statistics
/// </summary>
public class OrganizationOverviewDto
{
    /// <summary>
    /// Organization ID
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Organization name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Organization status (Active/Inactive)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Date when organization was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Total number of users in the organization
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Number of active users in the last 30 days
    /// </summary>
    public int ActiveUsersLast30Days { get; set; }

    /// <summary>
    /// Total number of transcription sessions
    /// </summary>
    public int TotalSessions { get; set; }

    /// <summary>
    /// Total number of saved transcriptions
    /// </summary>
    public int TotalTranscriptions { get; set; }
}

/// <summary>
/// User activity and statistics
/// </summary>
public class UserActivityDto
{
    /// <summary>
    /// Total number of users in the organization
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Number of active users in the last 7 days
    /// </summary>
    public int ActiveUsersLast7Days { get; set; }

    /// <summary>
    /// Number of active users in the last 30 days
    /// </summary>
    public int ActiveUsersLast30Days { get; set; }

    /// <summary>
    /// Number of users with admin role
    /// </summary>
    public int AdminUsers { get; set; }

    /// <summary>
    /// Number of users with regular role
    /// </summary>
    public int RegularUsers { get; set; }

    /// <summary>
    /// Number of pending user invitations
    /// </summary>
    public int PendingInvitations { get; set; }

    /// <summary>
    /// Recent user activity (last 5 users who logged in)
    /// </summary>
    public List<UserActivityItemDto> RecentUserActivity { get; set; } = new();
}

/// <summary>
/// Individual user activity item
/// </summary>
public class UserActivityItemDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User's email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the organization
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Last login date
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Whether the user is active
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Subscription status and usage information
/// </summary>
public class SubscriptionStatusDto
{
    /// <summary>
    /// Current subscription plan
    /// </summary>
    public string CurrentPlan { get; set; } = string.Empty;

    /// <summary>
    /// Subscription status (Active/Cancelled/Suspended)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Monthly transcription minutes limit
    /// </summary>
    public int MonthlyLimit { get; set; }

    /// <summary>
    /// Minutes used this month
    /// </summary>
    public int MinutesUsed { get; set; }

    /// <summary>
    /// Minutes remaining this month
    /// </summary>
    public int MinutesRemaining { get; set; }

    /// <summary>
    /// Usage percentage (0-100)
    /// </summary>
    public decimal UsagePercentage { get; set; }

    /// <summary>
    /// Whether usage is near the limit (within 2 hours)
    /// </summary>
    public bool IsNearLimit { get; set; }

    /// <summary>
    /// Date when usage resets
    /// </summary>
    public DateTime UsageResetDate { get; set; }

    /// <summary>
    /// Total usage across all time
    /// </summary>
    public int TotalUsage { get; set; }
}

/// <summary>
/// Transcription statistics and metrics
/// </summary>
public class TranscriptionStatsDto
{
    /// <summary>
    /// Total number of transcription sessions
    /// </summary>
    public int TotalSessions { get; set; }

    /// <summary>
    /// Number of sessions in the last 30 days
    /// </summary>
    public int SessionsLast30Days { get; set; }

    /// <summary>
    /// Total number of saved transcriptions
    /// </summary>
    public int TotalTranscriptions { get; set; }

    /// <summary>
    /// Number of transcriptions in the last 30 days
    /// </summary>
    public int TranscriptionsLast30Days { get; set; }

    /// <summary>
    /// Total transcription minutes across all time
    /// </summary>
    public int TotalTranscriptionMinutes { get; set; }

    /// <summary>
    /// Average session duration in minutes
    /// </summary>
    public decimal AverageSessionDuration { get; set; }

    /// <summary>
    /// Number of active sessions currently running
    /// </summary>
    public int ActiveSessions { get; set; }

    /// <summary>
    /// Most active speaker (based on transcription count)
    /// </summary>
    public string? MostActiveSpeaker { get; set; }
}

/// <summary>
/// Recent activity and events
/// </summary>
public class RecentActivityDto
{
    /// <summary>
    /// Recent transcription sessions (last 5)
    /// </summary>
    public List<RecentSessionDto> RecentSessions { get; set; } = new();

    /// <summary>
    /// Recent saved transcriptions (last 5)
    /// </summary>
    public List<RecentTranscriptionDto> RecentTranscriptions { get; set; } = new();

    /// <summary>
    /// Recent user activities (last 5)
    /// </summary>
    public List<RecentUserActivityDto> RecentUserActivities { get; set; } = new();
}

/// <summary>
/// Recent transcription session information
/// </summary>
public class RecentSessionDto
{
    /// <summary>
    /// Session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Session title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Session status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Session duration in minutes
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Date when session was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User who created the session
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Recent transcription information
/// </summary>
public class RecentTranscriptionDto
{
    /// <summary>
    /// Transcription ID
    /// </summary>
    public Guid TranscriptionId { get; set; }

    /// <summary>
    /// Transcription title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Speaker name
    /// </summary>
    public string Speaker { get; set; } = string.Empty;

    /// <summary>
    /// Transcription duration in minutes
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// Date when transcription was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; }

    /// <summary>
    /// User who created the transcription
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Recent user activity information
/// </summary>
public class RecentUserActivityDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Activity type (Login, Session Created, etc.)
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Activity description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Date when activity occurred
    /// </summary>
    public DateTime ActivityDate { get; set; }
}
