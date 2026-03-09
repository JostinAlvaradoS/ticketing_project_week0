using Microsoft.EntityFrameworkCore;
using Fulfillment.Domain.Entities;

namespace Fulfillment.Infrastructure.Persistence;

public class FulfillmentDbContext : DbContext
{
    public FulfillmentDbContext(DbContextOptions<FulfillmentDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Ticket entity
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets", schema: "bc_fulfillment");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.OrderId)
                .IsRequired();
            
            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(e => e.EventName)
                .HasMaxLength(500)
                .IsRequired();
            
            entity.Property(e => e.SeatNumber)
                .HasMaxLength(50)
                .IsRequired();
            
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("USD");
            
            entity.Property(e => e.QrCodeData)
                .HasMaxLength(1000)
                .IsRequired();
            
            entity.Property(e => e.TicketPdfPath)
                .HasMaxLength(1000)
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()");
            
            // Unique constraint on OrderId to prevent duplicates
            entity.HasIndex(e => e.OrderId)
                .IsUnique();
        });
    }
}
