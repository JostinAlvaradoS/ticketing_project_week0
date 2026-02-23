using Microsoft.EntityFrameworkCore;
using Identity.Domain.Ports;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Implementación del puerto IDbInitializer usando Entity Framework Core.
/// Responsable de aplicar las migraciones a la base de datos.
/// </summary>
public class DbInitializer : IDbInitializer
{
    private readonly IdentityDbContext _dbContext;

    public DbInitializer(IdentityDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Aplica todas las migraciones pendientes a la base de datos.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _dbContext.Database.MigrateAsync();
    }
}
