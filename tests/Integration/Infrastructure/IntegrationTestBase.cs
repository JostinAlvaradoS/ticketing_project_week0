using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Catalog.Infrastructure.Persistence;
using Ordering.Infrastructure.Persistence;

namespace TicketingFlow.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for integration tests that require multiple services and infrastructure.
/// Sets up Testcontainers for PostgreSQL and Redis, and provides HTTP clients for each service.
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer PostgreSqlContainer { get; private set; } = null!;
    protected RedisContainer RedisContainer { get; private set; } = null!;
    
    protected WebApplicationFactory<Catalog.Api.Program> CatalogFactory { get; private set; } = null!;
    protected WebApplicationFactory<Ordering.Api.Program> OrderingFactory { get; private set; } = null!;
    
    protected HttpClient CatalogClient { get; private set; } = null!;
    protected HttpClient OrderingClient { get; private set; } = null!;
    
    protected string PostgreConnectionString { get; private set; } = string.Empty;
    protected string RedisConnectionString { get; private set; } = string.Empty;

    public virtual async Task InitializeAsync()
    {
        // Start containers
        PostgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("ticketing_integration_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        RedisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await PostgreSqlContainer.StartAsync();
        await RedisContainer.StartAsync();

        PostgreConnectionString = PostgreSqlContainer.GetConnectionString();
        RedisConnectionString = RedisContainer.GetConnectionString();

        // Setup web application factories
        CatalogFactory = new WebApplicationFactory<Catalog.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = PostgreConnectionString,
                        ["ConnectionStrings:Redis"] = RedisConnectionString
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    // Replace the database context with test configuration
                    services.RemoveAll<DbContextOptions<CatalogDbContext>>();
                    services.AddDbContext<CatalogDbContext>(options =>
                    {
                        options.UseNpgsql(PostgreConnectionString, opt =>
                        {
                            opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        });
                        options.LogTo(Console.WriteLine);
                    });
                });
            });

        OrderingFactory = new WebApplicationFactory<Ordering.Api.Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = PostgreConnectionString,
                        ["ConnectionStrings:Redis"] = RedisConnectionString
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    // Replace the database context with test configuration
                    services.RemoveAll<DbContextOptions<OrderingDbContext>>();
                    services.AddDbContext<OrderingDbContext>(options =>
                    {
                        options.UseNpgsql(PostgreConnectionString, opt =>
                        {
                            opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        });
                        options.LogTo(Console.WriteLine);
                    });
                });
            });

        CatalogClient = CatalogFactory.CreateClient();
        OrderingClient = OrderingFactory.CreateClient();

        // Initialize databases
        await InitializeDatabasesAsync();
    }

    public virtual async Task DisposeAsync()
    {
        CatalogClient?.Dispose();
        OrderingClient?.Dispose();
        
        if (CatalogFactory != null)
            await CatalogFactory.DisposeAsync();
        if (OrderingFactory != null)
            await OrderingFactory.DisposeAsync();

        if (PostgreSqlContainer != null)
        {
            await PostgreSqlContainer.StopAsync();
            await PostgreSqlContainer.DisposeAsync();
        }

        if (RedisContainer != null)
        {
            await RedisContainer.StopAsync();
            await RedisContainer.DisposeAsync();
        }
    }

    private async Task InitializeDatabasesAsync()
    {
        // Apply catalog migrations and seed data
        using var catalogScope = CatalogFactory.Services.CreateScope();
        var catalogDb = catalogScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await catalogDb.Database.EnsureCreatedAsync();
        await SeedCatalogDataAsync(catalogDb);

        // Apply ordering migrations  
        using var orderingScope = OrderingFactory.Services.CreateScope();
        var orderingDb = orderingScope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await orderingDb.Database.EnsureCreatedAsync();
    }

    protected virtual async Task SeedCatalogDataAsync(CatalogDbContext context)
    {
        // Create a test event with seats
        var eventId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var testEvent = new Catalog.Domain.Entities.Event
        {
            Id = eventId,
            Name = "Test Concert",
            Description = "A test concert for integration testing",
            StartDate = DateTime.UtcNow.AddMonths(1),
            Venue = "Test Venue",
            MaxCapacity = 100
        };

        var testSeats = new List<Catalog.Domain.Entities.Seat>();
        for (int row = 1; row <= 10; row++)
        {
            for (int seatNum = 1; seatNum <= 10; seatNum++)
            {
                testSeats.Add(new Catalog.Domain.Entities.Seat
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    SectionCode = "A",
                    RowNumber = row,
                    SeatNumber = seatNum,
                    Price = 50.00m,
                    Status = "available"
                });
            }
        }

        context.Events.Add(testEvent);
        context.Seats.AddRange(testSeats);
        await context.SaveChangesAsync();
    }

    protected static async Task<T?> DeserializeHttpContent<T>(HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}