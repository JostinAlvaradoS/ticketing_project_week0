using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Inventory.Domain.Ports;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Locking;
using Inventory.Infrastructure.Messaging;
using StackExchange.Redis;
using Confluent.Kafka;

namespace Inventory.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"));
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
        services.AddScoped<IKafkaProducer, KafkaProducer>();

        return services;
    }
}
