using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fulfillment.Application.Ports;
using Fulfillment.Infrastructure.Persistence;
using Fulfillment.Infrastructure.Events;
using Fulfillment.Infrastructure.PdfGeneration;
using Fulfillment.Infrastructure.Services;

namespace Fulfillment.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Controllers
        services.AddControllers();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Fulfillment.Application.UseCases.ProcessPaymentSucceeded.ProcessPaymentSucceededHandler).Assembly));

        services.AddDbContext<FulfillmentDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_fulfillment"));
        });
        
        // IDbInitializer removed - migrations handled externally
        services.AddScoped<ITicketRepository, TicketRepository>();
        
        // External Services
        services.AddHttpClient<IOrderingServiceClient, OrderingServiceClient>(client =>
        {
            var url = configuration["OrderingService:Url"] ?? "http://speckit-ordering:5003";
            client.BaseAddress = new Uri(url);
        });
        
        // PDF & QR Services
        services.AddScoped<IQrCodeService, QrCodeService>();
        services.AddScoped<ITicketPdfGenerator, TicketPdfGenerator>();
        services.AddScoped<ITicketStorageService, LocalTicketStorageService>();
        
        // Kafka Publisher
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
        
        // Configure Kafka options
        services.Configure<KafkaOptions>(
            configuration.GetSection(KafkaOptions.Section));

        // Register Kafka consumer as hosted service
        services.AddHostedService<PaymentSucceededEventConsumer>();
        
        return services;
    }

    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        // Apply migrations automatically on startup
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
                dbContext.Database.Migrate();
                Console.WriteLine("✅ Fulfillment migrations applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning: Could not apply migrations: {ex.Message}");
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

        return app;
    }
}
