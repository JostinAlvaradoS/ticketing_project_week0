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
    /// Valida que el schema bc_identity existe antes de proceder.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Verificar que el schema "bc_identity" existe
            var schemaExists = await _dbContext.Database
                .SqlQueryRaw<bool>(
                    @"SELECT EXISTS (
                        SELECT 1 FROM information_schema.schemata 
                        WHERE schema_name = 'bc_identity'
                    )")
                .FirstOrDefaultAsync();

            if (!schemaExists)
            {
                throw new InvalidOperationException(
                    "Schema 'bc_identity' does not exist. " +
                    "Make sure you're connected to the correct database (ticketing) " +
                    "and that init-schemas.sql has been executed.");
            }

            // Aplicar migraciones
            await _dbContext.Database.MigrateAsync();
            
            Console.WriteLine("✓ Migraciones aplicadas correctamente en schema bc_identity");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error fatal en inicialización de BD: {ex.Message}");
            throw;
        }
    }
}

