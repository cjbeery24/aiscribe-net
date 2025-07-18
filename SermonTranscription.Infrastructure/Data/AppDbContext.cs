using Microsoft.EntityFrameworkCore;

namespace SermonTranscription.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets will be added when entities are created
    // public DbSet<User> Users { get; set; }
    // public DbSet<Organization> Organizations { get; set; }
    // public DbSet<TranscriptionSession> TranscriptionSessions { get; set; }
    // public DbSet<Transcription> Transcriptions { get; set; }
    // public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be applied here
        // modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
} 