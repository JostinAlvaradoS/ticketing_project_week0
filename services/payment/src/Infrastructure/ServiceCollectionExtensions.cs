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

namespace Payment.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Controllers
        services.AddControllers();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProcessPaymentHandler).Assembly));

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

    public static async Task<WebApplication> UseInfrastructure(this WebApplication app)
    {
        // Apply migrations automatically on startup
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
                await dbInitializer.InitializeAsync();
                Console.WriteLine("✓ Payment DB initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Warning: Could not initialize database");
                Console.WriteLine($"  Reason: {ex.Message}");
                Console.WriteLine($"  Service will continue to run. DB will be initialized on next startup.");
            }
        }

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