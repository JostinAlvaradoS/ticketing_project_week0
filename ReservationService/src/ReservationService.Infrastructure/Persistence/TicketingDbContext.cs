using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;

namespace ReservationService.Infrastructure.Persistence;

public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id).HasColumnName("id");
            entity.Property(t => t.EventId).HasColumnName("event_id");
            entity.Property(t => t.Status).HasColumnName("status")
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<TicketStatus>(v, true));
            entity.Property(t => t.ReservedAt).HasColumnName("reserved_at");
            entity.Property(t => t.ExpiresAt).HasColumnName("expires_at");
            entity.Property(t => t.PaidAt).HasColumnName("paid_at");
            entity.Property(t => t.OrderId).HasColumnName("order_id");
            entity.Property(t => t.ReservedBy).HasColumnName("reserved_by");
            entity.Property(t => t.Version).HasColumnName("version");
        });
    }
}
