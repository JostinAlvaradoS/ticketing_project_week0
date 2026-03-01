using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public DbSet<EmailNotification> EmailNotifications { get; set; } = null!;

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("bc_notification");

        modelBuilder.Entity<EmailNotification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever();

            entity.Property(e => e.RecipientEmail)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Body)
                .IsRequired();

            entity.Property(e => e.TicketPdfUrl)
                .HasMaxLength(1000);

            entity.Property(e => e.FailureReason)
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.Property(e => e.CreatedAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.SentAt)
                .HasColumnType("timestamp with time zone");

            entity.Property(e => e.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            // Index for quick lookups
            entity.HasIndex(e => e.OrderId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
