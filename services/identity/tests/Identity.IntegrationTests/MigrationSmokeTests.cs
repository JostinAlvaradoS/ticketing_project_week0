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
            .WithDatabase("identity_test")
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
            
            // Verificar que el schema existe
            var schemaExists = await dbContext.Database
                .SqlQueryRaw<bool>(
                    $@"SELECT EXISTS (
                        SELECT 1 FROM information_schema.schemata 
                        WHERE schema_name = 'bc_identity'
                    )")
                .FirstOrDefaultAsync();
            
            schemaExists.Should().Be(true);

            // Verificar que la tabla Users existe
            var userTableExists = await dbContext.Database
                .SqlQueryRaw<bool>(
                    $@"SELECT EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_schema = 'bc_identity' AND table_name = 'Users'
                    )")
                .FirstOrDefaultAsync();
            
            userTableExists.Should().Be(true);

            // Verificar que las columnas necesarias existen
            var columnsQuery = @"
                SELECT column_name FROM information_schema.columns 
                WHERE table_schema = 'bc_identity' AND table_name = 'Users'
                ORDER BY column_name";
            
            var columns = await dbContext.Database
                .SqlQueryRaw<string>(columnsQuery)
                .ToListAsync();
            
            columns.Should().Contain(new[] { "Id", "Email", "PasswordHash" });
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
