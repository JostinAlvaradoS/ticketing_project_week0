using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Ports;

namespace Inventory.Infrastructure.Persistence;

/// <summary>
/// Inicializa la base de datos para el módulo Inventory: crea schema si es necesario y aplica migraciones.
/// Sigue el mismo patrón usado por el servicio Identity para consistencia.
/// </summary>
public class DbInitializer : IDbInitializer
{
    private readonly InventoryDbContext _db;

    public DbInitializer(InventoryDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task InitializeAsync()
    {
        try
        {
            var connection = _db.Database.GetDbConnection();
            await connection.OpenAsync();

            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = @"SELECT EXISTS (
                SELECT 1 FROM information_schema.schemata 
                WHERE schema_name = 'bc_inventory'
            )";

            var result = await checkCommand.ExecuteScalarAsync();
            var schemaExists = (bool)(result ?? false);

            if (!schemaExists)
            {
                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"CREATE SCHEMA IF NOT EXISTS bc_inventory;";
                await createCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Schema 'bc_inventory' creado exitosamente");
            }

            await connection.CloseAsync();

            await _db.Database.MigrateAsync();

            Console.WriteLine("✓ Migraciones aplicadas correctamente en schema bc_inventory");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error fatal en inicialización de BD (Inventory): {ex.Message}");
            throw;
        }
    }
}
