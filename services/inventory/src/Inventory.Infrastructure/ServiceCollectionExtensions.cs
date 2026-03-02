using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Locking;
using Inventory.Infrastructure.Messaging;
using Inventory.Infrastructure.Consumers;
using StackExchange.Redis;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;

namespace Inventory.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_inventory"));
        });

        services.AddScoped<IDbInitializer, DbInitializer>();

        // Configure Redis connection multiplexer and Redis lock adapter
        var redisConn = configuration.GetConnectionString("Redis") ?? configuration["Redis:Connection"] ?? "localhost:6379";
        var multiplexer = ConnectionMultiplexer.Connect(redisConn);
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        services.AddScoped<IRedisLock, RedisLock>();

        // Configure Kafka producer
        var kafkaBootstrapServers = configuration.GetConnectionString("Kafka") ?? configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            AllowAutoCreateTopics = true,
            Acks = Acks.All
        };
        var producer = new ProducerBuilder<string?, string>(kafkaConfig).Build();
        services.AddSingleton(producer);
        services.AddSingleton<IKafkaProducer, KafkaProducer>();

        // Register inventory event consumer
        services.AddHostedService<Inventory.Infrastructure.Messaging.InventoryEventConsumer>();

        // Register expiry worker as hosted service (optional in tests)
        services.AddSingleton<IHostedService, Inventory.Infrastructure.Workers.ReservationExpiryWorker>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var kafka = sp.GetRequiredService<IKafkaProducer>();
            return new Inventory.Infrastructure.Workers.ReservationExpiryWorker(scopeFactory, kafka);
        });

        // Register seats-generated Kafka consumer as hosted service
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "inventory-seats-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        var consumer = new ConsumerBuilder<string?, string>(consumerConfig).Build();
        services.AddSingleton<IHostedService, SeatsGeneratedConsumer>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            return new SeatsGeneratedConsumer(scopeFactory, consumer);
        });

        return services;
    }
}
