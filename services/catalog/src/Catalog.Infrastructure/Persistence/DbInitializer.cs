using Microsoft.EntityFrameworkCore;
using Catalog.Application.Ports;

namespace Catalog.Infrastructure.Persistence;

/// <summary>
/// Implementación del puerto IDbInitializer usando Entity Framework Core.
/// Responsable de aplicar las migraciones a la base de datos del contexto Catalog.
/// </summary>
public class DbInitializer : IDbInitializer
{
    private readonly CatalogDbContext _dbContext;

    public DbInitializer(CatalogDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Aplica todas las migraciones pendientes a la base de datos.
    /// Crea el schema bc_catalog si no existe, luego aplica las migraciones.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Obtener conexión a la base de datos
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            
            // Verificar si el schema "bc_catalog" existe
            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = @"SELECT EXISTS (
                SELECT 1 FROM information_schema.schemata 
                WHERE schema_name = 'bc_catalog'
            )";
            
            var result = await checkCommand.ExecuteScalarAsync();
            var schemaExists = (bool)(result ?? false);
            
            // Si el schema no existe, crearlo
            if (!schemaExists)
            {
                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"CREATE SCHEMA IF NOT EXISTS bc_catalog;";
                await createCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Schema 'bc_catalog' creado exitosamente");
            }
            
            await connection.CloseAsync();

            // Aplicar migraciones
            await _dbContext.Database.MigrateAsync();
            
            Console.WriteLine("✓ Migraciones aplicadas correctamente en schema bc_catalog");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error fatal en inicialización de BD: {ex.Message}");
            throw;
        }
    }
}