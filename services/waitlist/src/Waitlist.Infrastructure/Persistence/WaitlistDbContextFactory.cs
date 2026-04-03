using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Waitlist.Infrastructure.Persistence;

/// <summary>
/// Allows EF Core tools (dotnet ef migrations) to discover the DbContext at design time.
/// </summary>
public class WaitlistDbContextFactory : IDesignTimeDbContextFactory<WaitlistDbContext>
{
    public WaitlistDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WaitlistDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=ticketing;Username=postgres;Password=postgres;SearchPath=bc_waitlist",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "bc_waitlist"))
            .Options;

        return new WaitlistDbContext(options);
    }
}
