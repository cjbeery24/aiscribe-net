using Bogus;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;

namespace SermonTranscription.Tests.Unit.Common;

/// <summary>
/// Factory class for generating test data using Bogus
/// </summary>
public static class TestDataFactory
{
    private static readonly Faker _faker = new();

    /// <summary>
    /// Generate a fake Organization with realistic data
    /// </summary>
    public static Faker<Organization> OrganizationFaker => new Faker<Organization>()
        .RuleFor(o => o.Id, f => f.Random.Guid())
        .RuleFor(o => o.Name, f => f.Company.CompanyName())
        .RuleFor(o => o.Slug, f => f.Internet.DomainWord())
        .RuleFor(o => o.Description, f => f.Lorem.Sentence())
        .RuleFor(o => o.ContactEmail, f => f.Internet.Email())
        .RuleFor(o => o.PhoneNumber, f => f.Phone.PhoneNumber())
        .RuleFor(o => o.Address, f => f.Address.FullAddress())
        .RuleFor(o => o.WebsiteUrl, f => f.Internet.Url())
        .RuleFor(o => o.CreatedAt, f => f.Date.Past())
        .RuleFor(o => o.UpdatedAt, (f, o) => f.Date.Between(o.CreatedAt, DateTime.UtcNow))
        .RuleFor(o => o.IsActive, f => f.Random.Bool(0.9f));

    /// <summary>
    /// Generate a fake User with realistic data
    /// </summary>
    public static Faker<User> UserFaker => new Faker<User>()
        .RuleFor(u => u.Id, f => f.Random.Guid())
        .RuleFor(u => u.FirstName, f => f.Person.FirstName)
        .RuleFor(u => u.LastName, f => f.Person.LastName)
        .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
        .RuleFor(u => u.PasswordHash, f => f.Internet.Password())
        .RuleFor(u => u.IsEmailVerified, f => f.Random.Bool(0.8f))
        .RuleFor(u => u.CreatedAt, f => f.Date.Past())
        .RuleFor(u => u.UpdatedAt, (f, u) => f.Date.Between(u.CreatedAt, DateTime.UtcNow))
        .RuleFor(u => u.LastLoginAt, (f, u) => f.Date.Between(u.CreatedAt, DateTime.UtcNow))
        .RuleFor(u => u.IsActive, f => f.Random.Bool(0.9f));

    /// <summary>
    /// Generate a fake UserOrganization relationship with realistic data
    /// </summary>
    public static Faker<UserOrganization> UserOrganizationFaker => new Faker<UserOrganization>()
        .RuleFor(uo => uo.UserId, f => f.Random.Guid())
        .RuleFor(uo => uo.OrganizationId, f => f.Random.Guid())
        .RuleFor(uo => uo.Role, f => f.PickRandom<UserRole>())
        .RuleFor(uo => uo.CreatedAt, f => f.Date.Past())
        .RuleFor(uo => uo.UpdatedAt, (f, uo) => f.Date.Between(uo.CreatedAt, DateTime.UtcNow))
        .RuleFor(uo => uo.IsActive, f => f.Random.Bool(0.9f))
        .RuleFor(uo => uo.InvitationAcceptedAt, (f, uo) => f.Random.Bool(0.8f) ? f.Date.Between(uo.CreatedAt, DateTime.UtcNow) : null)
        .RuleFor(uo => uo.InvitedByUserId, f => f.Random.Bool(0.5f) ? f.Random.Guid() : null);

    /// <summary>
    /// Generate a fake TranscriptionSession with realistic data
    /// </summary>
    public static Faker<TranscriptionSession> TranscriptionSessionFaker => new Faker<TranscriptionSession>()
        .RuleFor(ts => ts.Id, f => f.Random.Guid())
        .RuleFor(ts => ts.Title, f => f.Lorem.Sentence(3, 5))
        .RuleFor(ts => ts.Description, f => f.Lorem.Paragraph())
        .RuleFor(ts => ts.Status, f => f.PickRandom<SessionStatus>())
        .RuleFor(ts => ts.CreatedAt, f => f.Date.Past())
        .RuleFor(ts => ts.UpdatedAt, (f, ts) => f.Date.Between(ts.CreatedAt, DateTime.UtcNow))
        .RuleFor(ts => ts.StartedAt, (f, ts) => ts.Status != SessionStatus.Created ? f.Date.Between(ts.CreatedAt, DateTime.UtcNow) : null)
        .RuleFor(ts => ts.CompletedAt, (f, ts) => ts.Status == SessionStatus.Completed ? f.Date.Recent() : null)
        .RuleFor(ts => ts.AudioStreamUrl, f => f.Internet.Url())
        .RuleFor(ts => ts.Language, f => "en-US")
        .RuleFor(ts => ts.EnablePunctuation, f => f.Random.Bool(0.8f))
        .RuleFor(ts => ts.EnableSpeakerDiarization, f => f.Random.Bool(0.6f))
        .RuleFor(ts => ts.OrganizationId, f => f.Random.Guid())
        .RuleFor(ts => ts.CreatedByUserId, f => f.Random.Guid());

    /// <summary>
    /// Generate a fake Transcription with realistic data
    /// </summary>
    public static Faker<Transcription> TranscriptionFaker => new Faker<Transcription>()
        .RuleFor(t => t.Id, f => f.Random.Guid())
        .RuleFor(t => t.Title, f => f.Lorem.Sentence(4, 6))
        .RuleFor(t => t.Description, f => f.Lorem.Paragraph())
        .RuleFor(t => t.Content, f => f.Lorem.Paragraphs(3, 8))
        .RuleFor(t => t.Speaker, f => f.Person.FullName)
        .RuleFor(t => t.ProcessedAt, f => f.Date.Past())
        .RuleFor(t => t.CreatedAt, f => f.Date.Past())
        .RuleFor(t => t.UpdatedAt, (f, t) => f.Date.Between(t.CreatedAt, DateTime.UtcNow))
        .RuleFor(t => t.Tags, f => f.Lorem.Words(f.Random.Int(1, 5)).ToArray())
        .RuleFor(t => t.OrganizationId, f => f.Random.Guid())
        .RuleFor(t => t.CreatedByUserId, f => f.Random.Guid())
        .RuleFor(t => t.SessionId, f => f.Random.Guid());

    /// <summary>
    /// Generate a fake Subscription with realistic data
    /// </summary>
    public static Faker<Subscription> SubscriptionFaker => new Faker<Subscription>()
        .RuleFor(s => s.Id, f => f.Random.Guid())
        .RuleFor(s => s.Plan, f => f.PickRandom<SubscriptionPlan>())
        .RuleFor(s => s.Status, f => f.PickRandom<SubscriptionStatus>())
        .RuleFor(s => s.StartDate, f => f.Date.Past())
        .RuleFor(s => s.EndDate, (f, s) => f.Date.Between(s.StartDate, s.StartDate.AddYears(1)))
        .RuleFor(s => s.NextBillingDate, (f, s) => s.EndDate.HasValue ? f.Date.Between(DateTime.UtcNow, s.EndDate.Value) : f.Date.Future())
        .RuleFor(s => s.MonthlyPrice, (f, s) => GetPriceForPlan(s.Plan))
        .RuleFor(s => s.Currency, f => "USD")
        .RuleFor(s => s.StripeCustomerId, f => $"cus_{f.Random.AlphaNumeric(24)}")
        .RuleFor(s => s.StripeSubscriptionId, f => $"sub_{f.Random.AlphaNumeric(24)}")
        .RuleFor(s => s.MaxTranscriptionMinutes, (f, s) => GetMaxMinutesForPlan(s.Plan))
        .RuleFor(s => s.CanExportTranscriptions, (f, s) => s.Plan != SubscriptionPlan.Basic)
        .RuleFor(s => s.CreatedAt, f => f.Date.Past())
        .RuleFor(s => s.UpdatedAt, (f, s) => f.Date.Between(s.CreatedAt, DateTime.UtcNow))
        .RuleFor(s => s.OrganizationId, f => f.Random.Guid());

    /// <summary>
    /// Generate a fake TranscriptionSegment with realistic data
    /// </summary>
    public static Faker<TranscriptionSegment> TranscriptionSegmentFaker => new Faker<TranscriptionSegment>()
        .RuleFor(ts => ts.Id, f => f.Random.Guid())
        .RuleFor(ts => ts.Text, f => f.Lorem.Sentence())
        .RuleFor(ts => ts.StartTime, f => f.Random.Double(0, 3600))
        .RuleFor(ts => ts.EndTime, (f, ts) => ts.StartTime + f.Random.Double(1, 30))
        .RuleFor(ts => ts.SequenceNumber, f => f.Random.Int(1, 1000))
        .RuleFor(ts => ts.Confidence, f => f.Random.Double(0.7, 1.0))
        .RuleFor(ts => ts.Speaker, f => f.Random.Bool(0.7f) ? f.Person.FirstName : null)
        .RuleFor(ts => ts.TranscriptionId, f => f.Random.Guid());

    // Helper methods for subscription plans
    private static decimal GetPriceForPlan(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Basic => 48.00m,
        SubscriptionPlan.Professional => 80.00m,
        SubscriptionPlan.Enterprise => 112.00m,
        _ => 0m
    };

    private static int GetMaxMinutesForPlan(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Basic => 360,
        SubscriptionPlan.Professional => 600,
        SubscriptionPlan.Enterprise => 840,
        _ => 60
    };

    /// <summary>
    /// Create a complete organization with users and subscription using many-to-many relationship
    /// </summary>
    public static (Organization organization, List<User> users, List<UserOrganization> userOrganizations, Subscription subscription) CreateCompleteOrganization(int userCount = 3)
    {
        var organization = OrganizationFaker.Generate();
        var users = UserFaker.Generate(userCount);
        var userOrganizations = new List<UserOrganization>();

        // Create UserOrganization relationships
        for (int i = 0; i < users.Count; i++)
        {
            var user = users[i];
            var role = i == 0 ? UserRole.OrganizationAdmin : UserRole.OrganizationUser; // First user is admin

            var userOrganization = new UserOrganization
            {
                UserId = user.Id,
                OrganizationId = organization.Id,
                Role = role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                User = user,
                Organization = organization
            };

            userOrganizations.Add(userOrganization);
            user.UserOrganizations.Add(userOrganization);
        }

        var subscription = SubscriptionFaker.Generate();
        subscription.OrganizationId = organization.Id;
        subscription.Organization = organization;

        organization.UserOrganizations = userOrganizations;
        organization.Subscriptions = new List<Subscription> { subscription };

        return (organization, users, userOrganizations, subscription);
    }

    /// <summary>
    /// Create a user with membership in a specific organization
    /// </summary>
    public static (User user, UserOrganization membership) CreateUserWithOrganization(Guid organizationId, UserRole role = UserRole.OrganizationUser)
    {
        var user = UserFaker.Generate();
        var membership = new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = organizationId,
            Role = role,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            User = user
        };

        user.UserOrganizations.Add(membership);
        return (user, membership);
    }

    /// <summary>
    /// Create a transcription session with related transcriptions
    /// </summary>
    public static (TranscriptionSession session, List<Transcription> transcriptions) CreateSessionWithTranscriptions(
        Guid organizationId,
        Guid userId,
        int transcriptionCount = 2)
    {
        var session = TranscriptionSessionFaker.Generate();
        session.OrganizationId = organizationId;
        session.CreatedByUserId = userId;

        var transcriptions = TranscriptionFaker.Generate(transcriptionCount);
        foreach (var transcription in transcriptions)
        {
            transcription.OrganizationId = organizationId;
            transcription.CreatedByUserId = userId;
            transcription.SessionId = session.Id;
            transcription.Session = session;
        }

        session.Transcriptions = transcriptions;
        return (session, transcriptions);
    }
}
