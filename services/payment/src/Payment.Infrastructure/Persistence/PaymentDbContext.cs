using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options) { }

    public DbSet<Domain.Entities.Payment> Payments => Set<Domain.Entities.Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_payment");

        modelBuilder.Entity<Domain.Entities.Payment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderId).IsRequired();
            entity.Property(x => x.CustomerId).IsRequired();
            entity.Property(x => x.ReservationId).IsRequired(false);
            entity.Property(x => x.Amount).IsRequired().HasPrecision(18, 2);
            entity.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            entity.Property(x => x.PaymentMethod).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            entity.Property(x => x.ErrorCode).IsRequired(false).HasMaxLength(100);
            entity.Property(x => x.ErrorMessage).IsRequired(false).HasMaxLength(500);
            entity.Property(x => x.FailureReason).IsRequired(false).HasMaxLength(255);
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.ProcessedAt).IsRequired(false);
            entity.Property(x => x.IsSimulated).IsRequired().HasDefaultValue(true);
            entity.Property(x => x.SimulatedResponse).IsRequired(false);

            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.CustomerId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
        });
    }
}