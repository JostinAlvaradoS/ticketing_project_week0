using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Entities;

namespace Ordering.Infrastructure.Persistence;

public class OrderingDbContext : DbContext
{
    public OrderingDbContext(DbContextOptions<OrderingDbContext> options)
        : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_ordering");

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired(false);
            entity.Property(x => x.GuestToken).IsRequired(false);
            entity.Property(x => x.TotalAmount).IsRequired().HasPrecision(18, 2);
            entity.Property(x => x.State).IsRequired().HasDefaultValue("draft");
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.PaidAt).IsRequired(false);

            entity.Navigation(x => x.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(x => x.Items)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.GuestToken);
            entity.HasIndex(x => x.State);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderId).IsRequired();
            entity.Property(x => x.SeatId).IsRequired();
            entity.Property(x => x.Price).IsRequired().HasPrecision(18, 2);

            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.SeatId);
        });
    }
}