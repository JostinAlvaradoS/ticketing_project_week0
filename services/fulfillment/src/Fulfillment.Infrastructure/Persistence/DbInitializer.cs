using Microsoft.EntityFrameworkCore;
using Fulfillment.Application.Ports;

namespace Fulfillment.Infrastructure.Persistence;

public class DbInitializer : IDbInitializer
{
    private readonly FulfillmentDbContext _context;

    public DbInitializer(FulfillmentDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Apply any pending migrations
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                await _context.Database.MigrateAsync();
            }
            else
            {
                // Test the connection
                await _context.Database.CanConnectAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }
}
