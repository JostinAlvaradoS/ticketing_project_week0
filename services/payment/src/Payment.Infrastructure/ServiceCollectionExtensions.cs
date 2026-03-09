using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Payment.Application.Ports;
using Payment.Application.UseCases.ProcessPayment;
using Payment.Infrastructure.Events;
using Payment.Infrastructure.EventConsumers;
using Payment.Infrastructure.Messaging;
using Payment.Infrastructure.Persistence;
using Payment.Infrastructure.Services;
using System.Text.Json;

namespace Payment.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Controllers with JSON support for camelCase
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            });

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProcessPaymentHandler).Assembly));

        // Database services
        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_payment"));
        });
        
        // IDbInitializer removed - migrations handled externally
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
        
        services.AddSingleton(sp => new ProducerBuilder<string?, string>(kafkaConfig).Build());
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

    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        // DB initialization and migrations are now handled externally (pipeline)
        
        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCors("FrontendPolicy");
        app.UseRouting();
        app.MapControllers();

        Console.WriteLine("🚀 Payment API is starting...");
        Console.WriteLine("📍 Listening on http://0.0.0.0:5004");
        Console.Out.Flush();

        return app;
    }
}