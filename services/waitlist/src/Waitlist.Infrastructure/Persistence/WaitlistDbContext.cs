using Microsoft.EntityFrameworkCore;
using Waitlist.Domain.Entities;

namespace Waitlist.Infrastructure.Persistence;

public class WaitlistDbContext : DbContext
{
    public WaitlistDbContext(DbContextOptions<WaitlistDbContext> options) : base(options) { }

    public DbSet<WaitlistEntry> WaitlistEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_waitlist");

        modelBuilder.Entity<WaitlistEntry>(eb =>
        {
            eb.HasKey(e => e.Id);
            eb.ToTable("waitlist_entries");
            eb.Property(e => e.Email).HasMaxLength(256).IsRequired();
            eb.Property(e => e.Status).HasMaxLength(20).IsRequired();
            eb.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // FIFO ordering index for queue position queries
            eb.HasIndex(e => new { e.EventId, e.Status, e.CreatedAt })
              .HasDatabaseName("idx_waitlist_fifo");

            // Partial index for active (Pending + Assigned) entries — duplicate check
            eb.HasIndex(e => new { e.Email, e.EventId, e.Status })
              .HasDatabaseName("idx_waitlist_active");

            // Index to find assigned entries for expiry worker
            eb.HasIndex(e => new { e.Status, e.ExpiresAt })
              .HasDatabaseName("idx_waitlist_expiry");
        });
    }
}
