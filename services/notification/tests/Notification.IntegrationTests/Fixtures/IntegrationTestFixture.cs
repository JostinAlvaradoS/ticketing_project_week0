using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Notification.Infrastructure.Persistence;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace Notification.IntegrationTests.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    private KafkaContainer? _kafkaContainer;
    private ServiceProvider? _serviceProvider;
    public NotificationDbContext? DbContext { get; private set; }
    public IServiceProvider? ServiceProvider => _serviceProvider;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .WithDatabase("speckit")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgresContainer.StartAsync();

        // Start Kafka container
        _kafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.5.0")
            .Build();

        await _kafkaContainer.StartAsync();

        // Build services
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(_postgresContainer, _kafkaContainer);

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(_postgresContainer.GetConnectionString(),
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_notification"));
        });

        services.AddScoped<IDbInitializer, DbInitializer>();

        _serviceProvider = services.BuildServiceProvider();

        // Initialize database
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            await dbInitializer.InitializeAsync();
        }

        // Get DbContext for tests
        DbContext = _serviceProvider.GetRequiredService<NotificationDbContext>();

        // Create Kafka topic
        await CreateKafkaTopicAsync("ticket-issued");
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider != null)
        {
            _serviceProvider.Dispose();
        }

        if (_postgresContainer != null)
        {
            await _postgresContainer.StopAsync();
            await _postgresContainer.DisposeAsync();
        }

        if (_kafkaContainer != null)
        {
            await _kafkaContainer.StopAsync();
            await _kafkaContainer.DisposeAsync();
        }
    }

    private IConfiguration BuildConfiguration(PostgreSqlContainer postgresContainer, KafkaContainer kafkaContainer)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:Default", postgresContainer.GetConnectionString() },
                { "Kafka:BootstrapServers", kafkaContainer.GetBootstrapAddress() },
                { "Kafka:ConsumerGroupId", "notification-service-tests" },
                { "Kafka:Topics:TicketIssued", "ticket-issued" },
                { "Email:Smtp:UseDevMode", "true" }
            }!)
            .Build();
    }

    private async Task CreateKafkaTopicAsync(string topicName)
    {
        // Using kafka-topics script would require executing commands in the container
        // For testing purposes, topics are auto-created by Kafka on first message
        // This is a placeholder for now
        await Task.Completed;
    }
}
