using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Application.Ports;
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

        // IDbInitializer removed - migrations handled externally

        // Register repositories
        services.AddScoped<ISeatRepository, SeatRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();

        // Configure Redis connection multiplexer and Redis lock adapter
        var redisConn = configuration.GetConnectionString("Redis") ?? configuration["Redis:Connection"] ?? "localhost:6379";
        
        // Lazy registration for Redis to avoid connection on EF build
        services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConn));
        services.AddScoped<IRedisLock, RedisLock>();

        // Configure Kafka producer
        var kafkaBootstrapServers = configuration.GetConnectionString("Kafka") ?? configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            AllowAutoCreateTopics = true,
            Acks = Acks.All
        };
        
        services.AddSingleton(sp => new ProducerBuilder<string?, string>(kafkaConfig).Build());
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

        // Register payment-failed consumer (releases seats on failed payments)
        services.AddSingleton<IHostedService, Inventory.Infrastructure.Consumers.PaymentFailedConsumer>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Inventory.Infrastructure.Consumers.PaymentFailedConsumer>>();
            return new Inventory.Infrastructure.Consumers.PaymentFailedConsumer(scopeFactory, logger, kafkaBootstrapServers);
        });

        // Register seats-generated Kafka consumer as hosted service
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "inventory-seats-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        
        services.AddSingleton<IHostedService, SeatsGeneratedConsumer>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var consumer = new ConsumerBuilder<string?, string>(consumerConfig).Build();
            return new SeatsGeneratedConsumer(scopeFactory, consumer);
        });

        return services;
    }
}
