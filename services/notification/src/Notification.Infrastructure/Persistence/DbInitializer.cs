using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Notification.Infrastructure.Persistence;

public interface IDbInitializer
{
    Task InitializeAsync();
}

public class DbInitializer : IDbInitializer
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(NotificationDbContext context, ILogger<DbInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Applying Notification migrations...");
            await _context.Database.MigrateAsync();
            _logger.LogInformation("Notification migrations applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error applying migrations: {ex.Message}");
            throw;
        }
    }
}
