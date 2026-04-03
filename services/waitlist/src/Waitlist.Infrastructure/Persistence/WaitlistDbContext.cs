using Microsoft.EntityFrameworkCore;
using Waitlist.Domain.Entities;

namespace Waitlist.Infrastructure.Persistence;

public class WaitlistDbContext : DbContext
{
    public WaitlistDbContext(DbContextOptions<WaitlistDbContext> options) : base(options) { }

    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_waitlist");

        modelBuilder.Entity<WaitlistEntry>(e =>
        {
            e.ToTable("waitlist_entries");
            e.HasKey(x => x.Id);

            e.Property(x => x.Email).IsRequired().HasMaxLength(320);
            e.Property(x => x.EventId).IsRequired();
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.Property(x => x.RegisteredAt).IsRequired();

            // FIFO index
            e.HasIndex(x => new { x.EventId, x.Status, x.RegisteredAt })
             .HasDatabaseName("idx_waitlist_fifo");

            // Expiry worker index
            e.HasIndex(x => x.ExpiresAt)
             .HasFilter("\"Status\" = 'assigned'")
             .HasDatabaseName("idx_waitlist_expiry");

            // OrderId lookup index
            e.HasIndex(x => x.OrderId)
             .HasFilter("\"OrderId\" IS NOT NULL")
             .HasDatabaseName("idx_waitlist_order");
        });
    }
}
