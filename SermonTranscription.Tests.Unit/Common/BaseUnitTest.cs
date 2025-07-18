using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SermonTranscription.Infrastructure.Data;

namespace SermonTranscription.Tests.Unit.Common;

/// <summary>
/// Base class for unit tests providing common setup and utilities
/// </summary>
public abstract class BaseUnitTest : IDisposable
{
    protected readonly ServiceProvider ServiceProvider;
    protected readonly AppDbContext DbContext;

    protected BaseUnitTest()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<AppDbContext>();
    }

    /// <summary>
    /// Configure services for the test. Override to add custom services.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add in-memory database
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
    }

    /// <summary>
    /// Get a service from the DI container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Get a service from the DI container, or null if not registered
    /// </summary>
    protected T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    /// <summary>
    /// Create a new database context with the same configuration
    /// </summary>
    protected AppDbContext CreateNewDbContext()
    {
        var options = ServiceProvider.GetRequiredService<DbContextOptions<AppDbContext>>();
        return new AppDbContext(options);
    }

    /// <summary>
    /// Clear all data from the database
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Save changes and detach all entities to ensure fresh state
    /// </summary>
    protected async Task<int> SaveAndDetachAllAsync()
    {
        var result = await DbContext.SaveChangesAsync();
        
        // Detach all entities to ensure fresh state for subsequent operations
        foreach (var entry in DbContext.ChangeTracker.Entries().ToArray())
        {
            entry.State = EntityState.Detached;
        }
        
        return result;
    }

    public void Dispose()
    {
        DbContext?.Dispose();
        ServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
} 