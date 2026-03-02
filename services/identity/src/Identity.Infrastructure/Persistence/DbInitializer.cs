using Microsoft.EntityFrameworkCore;
using Identity.Domain.Ports;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Npgsql;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Implementación del puerto IDbInitializer usando Entity Framework Core.
/// Responsable de aplicar las migraciones a la base de datos.
/// </summary>
public class DbInitializer : IDbInitializer
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public DbInitializer(IdentityDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
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
            
            try
            {
                // Crear schema con permisos necesarios
                using var createCommand = connection.CreateCommand();
                createCommand.CommandText = @"
                    CREATE SCHEMA IF NOT EXISTS bc_identity;
                    ALTER SCHEMA bc_identity OWNER TO postgres;
                    GRANT ALL PRIVILEGES ON SCHEMA bc_identity TO postgres;
                ";
                await createCommand.ExecuteNonQueryAsync();
                Console.WriteLine("✓ Schema 'bc_identity' verificado/creado exitosamente");
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
                        SET search_path = bc_identity, public;
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
            
            Console.WriteLine("✓ Migraciones aplicadas correctamente en schema bc_identity");
            
            // Seed admin user automáticamente
            await SeedDefaultAdminUserAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error fatal en inicialización de BD: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Crea un usuario administrador predeterminado si no existe ningún admin en el sistema.
    /// Email: admin@ticketing.com, Password: Admin123!
    /// </summary>
    private async Task SeedDefaultAdminUserAsync()
    {
        try
        {
            // Verificar si ya existe algún usuario admin
            var hasAdmin = await _dbContext.Users
                .AnyAsync(u => u.Role == Role.Admin);

            if (!hasAdmin)
            {
                // Crear usuario admin predeterminado
                var passwordHash = _passwordHasher.HashPassword("Admin123!");
                var adminUser = new User(
                    email: "admin@ticketing.com", 
                    passwordHash: passwordHash, 
                    role: Role.Admin);

                _dbContext.Users.Add(adminUser);
                await _dbContext.SaveChangesAsync();
                
                Console.WriteLine("✓ Usuario administrador predeterminado creado:");
                Console.WriteLine("   Email: admin@ticketing.com");
                Console.WriteLine("   Password: Admin123!");
            }
            else
            {
                Console.WriteLine("✓ Usuario administrador ya existe en el sistema");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Warning al crear usuario admin: {ex.Message}");
            // No lanzar excepción aquí para no detener el inicio del servicio
        }
    }
}