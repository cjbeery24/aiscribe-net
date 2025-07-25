using Microsoft.EntityFrameworkCore;
using SermonTranscription.Domain.Entities;
using System.Reflection;

namespace SermonTranscription.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Domain Entity DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<UserOrganization> UserOrganizations { get; set; }
    public DbSet<TranscriptionSession> TranscriptionSessions { get; set; }
    public DbSet<Transcription> Transcriptions { get; set; }
    public DbSet<TranscriptionSegment> TranscriptionSegments { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global configuration
        ConfigureConventions(modelBuilder);
        ConfigureRelationships(modelBuilder);
        ConfigureIndexes(modelBuilder);
        SeedData(modelBuilder);
    }

    private static void ConfigureConventions(ModelBuilder modelBuilder)
    {
        // Set default string column types and lengths
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                // Set default string length to prevent nvarchar(max)
                if (property.ClrType == typeof(string) && property.GetMaxLength() == null)
                {
                    property.SetMaxLength(500);
                }

                // Configure DateTime properties to use UTC
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                }
            }
        }

        // Configure enums to be stored as strings
        modelBuilder.Entity<TranscriptionSession>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Subscription>()
            .Property(e => e.Plan)
            .HasConversion<string>();

        modelBuilder.Entity<Subscription>()
            .Property(e => e.Status)
            .HasConversion<string>();
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // User <-> Organization (Many-to-Many through UserOrganization)
        modelBuilder.Entity<UserOrganization>()
            .HasKey(uo => new { uo.UserId, uo.OrganizationId });

        modelBuilder.Entity<UserOrganization>()
            .HasOne(uo => uo.User)
            .WithMany(u => u.UserOrganizations)
            .HasForeignKey(uo => uo.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserOrganization>()
            .HasOne(uo => uo.Organization)
            .WithMany(o => o.UserOrganizations)
            .HasForeignKey(uo => uo.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing relationship for invitation tracking
        modelBuilder.Entity<UserOrganization>()
            .HasOne(uo => uo.InvitedByUser)
            .WithMany()
            .HasForeignKey(uo => uo.InvitedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // TranscriptionSession -> Organization (Many-to-One)
        modelBuilder.Entity<TranscriptionSession>()
            .HasOne(ts => ts.Organization)
            .WithMany(o => o.TranscriptionSessions)
            .HasForeignKey(ts => ts.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // TranscriptionSession -> User (Many-to-One)
        modelBuilder.Entity<TranscriptionSession>()
            .HasOne(ts => ts.CreatedByUser)
            .WithMany(u => u.TranscriptionSessions)
            .HasForeignKey(ts => ts.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transcription -> Organization (Many-to-One)
        modelBuilder.Entity<Transcription>()
            .HasOne(t => t.Organization)
            .WithMany(o => o.Transcriptions)
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transcription -> User (Many-to-One)
        modelBuilder.Entity<Transcription>()
            .HasOne(t => t.CreatedByUser)
            .WithMany(u => u.CreatedTranscriptions)
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transcription -> TranscriptionSession (Many-to-One, Optional)
        modelBuilder.Entity<Transcription>()
            .HasOne(t => t.Session)
            .WithMany(ts => ts.Transcriptions)
            .HasForeignKey(t => t.SessionId)
            .OnDelete(DeleteBehavior.SetNull);

        // TranscriptionSegment -> Transcription (Many-to-One)
        modelBuilder.Entity<TranscriptionSegment>()
            .HasOne(ts => ts.Transcription)
            .WithMany(t => t.Segments)
            .HasForeignKey(ts => ts.TranscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Subscription -> Organization (Many-to-One)
        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.Organization)
            .WithMany(o => o.Subscriptions)
            .HasForeignKey(s => s.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> RefreshToken (One-to-Many)
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // User indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // UserOrganization indexes
        modelBuilder.Entity<UserOrganization>()
            .HasIndex(uo => uo.UserId);

        modelBuilder.Entity<UserOrganization>()
            .HasIndex(uo => uo.OrganizationId);

        modelBuilder.Entity<UserOrganization>()
            .HasIndex(uo => uo.InvitationToken)
            .IsUnique();

        // Organization indexes
        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.Slug)
            .IsUnique();

        // TranscriptionSession indexes
        modelBuilder.Entity<TranscriptionSession>()
            .HasIndex(ts => ts.OrganizationId);

        modelBuilder.Entity<TranscriptionSession>()
            .HasIndex(ts => ts.CreatedByUserId);

        modelBuilder.Entity<TranscriptionSession>()
            .HasIndex(ts => ts.Status);

        modelBuilder.Entity<TranscriptionSession>()
            .HasIndex(ts => ts.CreatedAt);

        // Transcription indexes
        modelBuilder.Entity<Transcription>()
            .HasIndex(t => t.OrganizationId);

        modelBuilder.Entity<Transcription>()
            .HasIndex(t => t.CreatedByUserId);

        modelBuilder.Entity<Transcription>()
            .HasIndex(t => t.SessionId);

        modelBuilder.Entity<Transcription>()
            .HasIndex(t => t.CreatedAt);

        // TranscriptionSegment indexes
        modelBuilder.Entity<TranscriptionSegment>()
            .HasIndex(ts => ts.TranscriptionId);

        modelBuilder.Entity<TranscriptionSegment>()
            .HasIndex(ts => ts.SequenceNumber);

        // Subscription indexes
        modelBuilder.Entity<Subscription>()
            .HasIndex(s => s.OrganizationId);

        modelBuilder.Entity<Subscription>()
            .HasIndex(s => s.Status);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed default organization for development
        var defaultOrgId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        modelBuilder.Entity<Organization>().HasData(
            new Organization
            {
                Id = defaultOrgId,
                Name = "Default Organization",
                Slug = "default",
                Description = "Default organization for development",
                ContactEmail = "admin@example.com",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                MaxTranscriptionMinutes = 600,
                CanExportTranscriptions = true,
                HasRealtimeTranscription = true
            }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
            {
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Added &&
                entry.Entity.GetType().GetProperty("CreatedAt") != null)
            {
                entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}
