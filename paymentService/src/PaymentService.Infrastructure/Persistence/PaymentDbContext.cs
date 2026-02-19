using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<TicketHistory> TicketHistory { get; set; }
    public DbSet<Event> Events { get; set; }

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.HasPostgresEnum<TicketStatus>();
        modelBuilder.HasPostgresEnum<PaymentStatus>();

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.StartsAt).HasColumnName("starts_at");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.EventId).HasColumnName("event_id");
            entity.Property(t => t.Status).HasColumnName("status")
                .HasConversion(v => v.ToString(), v => Enum.Parse<TicketStatus>(v, true));
            entity.Property(t => t.ReservedAt).HasColumnName("reserved_at");
            entity.Property(t => t.ExpiresAt).HasColumnName("expires_at");
            entity.Property(t => t.PaidAt).HasColumnName("paid_at");
            entity.Property(t => t.OrderId).HasColumnName("order_id");
            entity.Property(t => t.ReservedBy).HasColumnName("reserved_by");
            entity.Property(t => t.Version).HasColumnName("version");

            entity.HasOne(t => t.Event)
                .WithMany(e => e.Tickets)
                .HasForeignKey(t => t.EventId);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasColumnName("id");
            entity.Property(p => p.TicketId).HasColumnName("ticket_id");
            entity.Property(p => p.Status).HasColumnName("status")
                .HasConversion(v => v.ToString(), v => Enum.Parse<PaymentStatus>(v, true));
            entity.Property(p => p.ProviderRef).HasColumnName("provider_ref");
            entity.Property(p => p.AmountCents).HasColumnName("amount_cents");
            entity.Property(p => p.Currency).HasColumnName("currency");
            entity.Property(p => p.CreatedAt).HasColumnName("created_at");
            entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(p => p.Ticket)
                .WithMany(t => t.Payments)
                .HasForeignKey(p => p.TicketId);
        });

        modelBuilder.Entity<TicketHistory>(entity =>
        {
            entity.ToTable("ticket_history");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Id).HasColumnName("id");
            entity.Property(h => h.TicketId).HasColumnName("ticket_id");
            entity.Property(h => h.OldStatus).HasColumnName("old_status")
                .HasConversion(v => v.ToString(), v => Enum.Parse<TicketStatus>(v, true));
            entity.Property(h => h.NewStatus).HasColumnName("new_status")
                .HasConversion(v => v.ToString(), v => Enum.Parse<TicketStatus>(v, true));
            entity.Property(h => h.ChangedAt).HasColumnName("changed_at");
            entity.Property(h => h.Reason).HasColumnName("reason");

            entity.HasOne(h => h.Ticket)
                .WithMany(t => t.History)
                .HasForeignKey(h => h.TicketId);
        });
    }
}
