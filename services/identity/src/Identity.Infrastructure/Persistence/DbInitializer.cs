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
    /// Crea el schema bc_identity si no existe, luego aplica las migraciones.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Obtener conexión a la base de datos
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            
            // Verificar si el schema "bc_identity" existe
            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = @"SELECT EXISTS (
                SELECT 1 FROM information_schema.schemata 
                WHERE schema_name = 'bc_identity'
            )";
            
            var result = await checkCommand.ExecuteScalarAsync();
            var schemaExists = (bool)(result ?? false);
            
            // Si el schema no existe, crearlo
            if (!schemaExists)
            {
                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"CREATE SCHEMA IF NOT EXISTS bc_identity;";
                await createCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Schema 'bc_identity' creado exitosamente");
            }
            
            await connection.CloseAsync();

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

