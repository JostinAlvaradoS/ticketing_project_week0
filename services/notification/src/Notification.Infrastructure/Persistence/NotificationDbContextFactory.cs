using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Notification.Infrastructure.Persistence;

/// <summary>
/// Factory para crear instancias de NotificationDbContext en tiempo de diseño (migraciones).
/// Esta clase permite que Entity Framework Core pueda crear el contexto sin necesidad
/// del contenedor de inyección de dependencias durante las operaciones de migración.
/// </summary>
public class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NotificationDbContext>();
        
        // Configuración para tiempo de diseño (migraciones) usando variables de entorno
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "ticketing";
        var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
        var schema = Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "bc_notification";
        
        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SearchPath={schema}";
        
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", schema)
        );

        return new NotificationDbContext(optionsBuilder.Options);
    }
}