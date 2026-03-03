using Microsoft.EntityFrameworkCore;
using Ordering.Application.Ports;
using Npgsql;

namespace Ordering.Infrastructure.Persistence;

/// <summary>
/// Implementación del puerto IDbInitializer usando Entity Framework Core.
/// Responsable de aplicar las migraciones a la base de datos del contexto Ordering.
/// </summary>
public class DbInitializer : IDbInitializer
{
    private readonly OrderingDbContext _dbContext;

    public DbInitializer(OrderingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Aplica todas las migraciones pendientes a la base de datos.
    /// Crea el schema bc_ordering si no existe, luego aplica las migraciones.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Obtener conexión a la base de datos
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            
            try
            {
                // Crear schema con permisos necesarios
                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"
                    CREATE SCHEMA IF NOT EXISTS bc_ordering;
                    ALTER SCHEMA bc_ordering OWNER TO postgres;
                    GRANT ALL PRIVILEGES ON SCHEMA bc_ordering TO postgres;
                ";
                await createCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Schema 'bc_ordering' verificado/creado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning al crear schema: {ex.Message}");
            }
            
            await connection.CloseAsync();

            // Aplicar migraciones
            try
            {
                await _dbContext.Database.MigrateAsync();
            }
            catch (PostgresException pex) when (pex.SqlState == "42P01")
            {
                // Table doesn't exist, create migrations history table manually
                Console.WriteLine("↻ Creando tabla de migraciones manualmente...");
                await connection.OpenAsync();
                try
                {
                    using var createTableCommand = connection.CreateCommand();
                    createTableCommand.CommandText = @"
                        SET search_path = bc_ordering, public;
                        CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                            MigrationId CHARACTER VARYING(150) NOT NULL PRIMARY KEY,
                            ProductVersion CHARACTER VARYING(32) NOT NULL
                        );
                        SET search_path = public;
                    ";
                    await createTableCommand.ExecuteNonQueryAsync();
                    Console.WriteLine("✓ Tabla de migraciones creada");
                }
                finally
                {
                    await connection.CloseAsync();
                }
                
                // Retry migrations after creating the history table
                await _dbContext.Database.MigrateAsync();
            }
            
            Console.WriteLine("✓ Migraciones aplicadas correctamente en schema bc_ordering");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error fatal en inicialización de BD: {ex.Message}");
            throw;
        }
    }
}