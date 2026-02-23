using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Seat> Seats { get; set; } = null!;
    public DbSet<Reservation> Reservations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_inventory");

        modelBuilder.Entity<Seat>(eb =>
        {
            eb.HasKey(e => e.Id);
            eb.Property(e => e.Version).IsRowVersion();
        });

        modelBuilder.Entity<Reservation>(eb =>
        {
            eb.HasKey(e => e.Id);
            eb.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            eb.Property(e => e.Status).HasMaxLength(20);
            eb.Property(e => e.CustomerId).HasMaxLength(256);
            eb.HasIndex(e => e.SeatId);
            eb.HasIndex(e => e.ExpiresAt);
        });
    }
}
