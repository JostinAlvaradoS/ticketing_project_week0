using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Ports;
using Payment.Infrastructure.Events;
using Payment.Infrastructure.EventConsumers;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database services
        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_payment"));
        });
        
        services.AddScoped<IDbInitializer, DbInitializer>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        
        // Configure Kafka options
        services.Configure<KafkaOptions>(
            configuration.GetSection(KafkaOptions.Section));

        // Configure Kafka producer
        var kafkaBootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";
        var kafkaConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            Acks = Acks.Leader,
            MessageTimeoutMs = 5000
        };
        
        var producer = new ProducerBuilder<string?, string>(kafkaConfig).Build();
        services.AddSingleton(producer);
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        
        // Event-driven service validation (replaces HTTP clients)
        services.AddSingleton<ReservationStateStore>();
        services.AddScoped<IOrderValidationService, EventBasedOrderValidationService>();
        services.AddScoped<IReservationValidationService, EventBasedReservationValidationService>();
        
        // Kafka event consumers
        services.AddHostedService<ReservationEventConsumer>();
        
        // Payment simulation
        services.AddScoped<IPaymentSimulatorService, PaymentSimulatorService>();

        return services;
    }
}