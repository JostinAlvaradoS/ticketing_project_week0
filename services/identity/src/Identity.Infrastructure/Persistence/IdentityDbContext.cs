using Microsoft.EntityFrameworkCore;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_identity");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(255);
            entity.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
            
            // Configuración del enum Role como entero en la BD
            entity.Property(x => x.Role)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(Role.User);

            // Índice único en Email para evitar duplicados
            entity.HasIndex(x => x.Email).IsUnique();
        });
    }
}