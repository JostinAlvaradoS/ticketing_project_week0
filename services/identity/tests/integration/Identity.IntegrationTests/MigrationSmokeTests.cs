using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;
using Identity.Infrastructure.Persistence;
using Identity.Domain.Ports;

namespace Identity.IntegrationTests;

[Trait("Category", "SmokeTest")]
public class MigrationSmokeTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("ticketing")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();
        _connectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    [Fact]
    public async Task MigrationsApply_Successfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString
            })
            .Build();

        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(_connectionString);
        });
        services.AddScoped<IDbInitializer, DbInitializer>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await dbInitializer.InitializeAsync();
        }

        // Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            
            // Verificar que el schema existe
            using var schemaCommand = connection.CreateCommand();
            schemaCommand.CommandText = @"SELECT EXISTS (
                SELECT 1 FROM information_schema.schemata 
                WHERE schema_name = 'bc_identity'
            )";
            var schemaExists = (bool)(await schemaCommand.ExecuteScalarAsync() ?? false);
            schemaExists.Should().Be(true);

            // Verificar que la tabla Users existe
            using var tableCommand = connection.CreateCommand();
            tableCommand.CommandText = @"SELECT EXISTS (
                SELECT 1 FROM information_schema.tables 
                WHERE table_schema = 'bc_identity' AND table_name = 'Users'
            )";
            var userTableExists = (bool)(await tableCommand.ExecuteScalarAsync() ?? false);
            userTableExists.Should().Be(true);

            // Verificar que las columnas necesarias existen
            using var columnsCommand = connection.CreateCommand();
            columnsCommand.CommandText = @"
                SELECT array_agg(column_name ORDER BY column_name)::text[] 
                FROM information_schema.columns 
                WHERE table_schema = 'bc_identity' AND table_name = 'Users'";
            
            var columnsResult = await columnsCommand.ExecuteScalarAsync();
            var columns = (columnsResult as string[] ?? Array.Empty<string>()).ToList();
            
            columns.Should().Contain(new[] { "Id", "Email", "PasswordHash" });
            
            await connection.CloseAsync();
        }
    }

    [Fact]
    public async Task DbContext_CanQueryUsers_AfterMigration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseNpgsql(_connectionString);
        });
        services.AddScoped<IDbInitializer, DbInitializer>();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        using (var scope = serviceProvider.CreateScope())
        {
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await dbInitializer.InitializeAsync();
        }

        // Assert - Can query without errors
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            
            // No exception should be thrown
            var userCount = await dbContext.Users.CountAsync();
            userCount.Should().Be(0);
        }
    }
}
