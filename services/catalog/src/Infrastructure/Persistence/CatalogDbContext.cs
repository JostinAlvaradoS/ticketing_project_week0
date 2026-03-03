using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Entities;

namespace Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
        : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_catalog");

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.Description).IsRequired();
            entity.Property(x => x.EventDate).IsRequired();
            entity.Property(x => x.Venue).IsRequired();
            entity.Property(x => x.MaxCapacity).IsRequired();
            entity.Property(x => x.BasePrice).IsRequired().HasPrecision(18, 2);
            entity.Property(x => x.Status).IsRequired().HasDefaultValue("active");
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt);
            entity.HasMany(x => x.Seats)
                .WithOne(x => x.Event)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventId).IsRequired();
            entity.Property(x => x.SectionCode).IsRequired();
            entity.Property(x => x.RowNumber).IsRequired();
            entity.Property(x => x.SeatNumber).IsRequired();
            entity.Property(x => x.Price).IsRequired().HasPrecision(18, 2);
            entity.Property(x => x.Status).IsRequired().HasDefaultValue("available");
            entity.Property(x => x.CurrentReservationId);
            entity.HasIndex(x => new { x.EventId, x.SectionCode, x.RowNumber, x.SeatNumber })
                .IsUnique();
        });
    }
}
