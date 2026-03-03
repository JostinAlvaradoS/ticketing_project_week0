using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ordering.Application.Ports;
using Ordering.Application.UseCases.AddToCart;
using Ordering.Infrastructure.Events;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Controllers
        services.AddControllers();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AddToCartHandler).Assembly));

        services.AddDbContext<OrderingDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_ordering"));
        });
        
        services.AddScoped<IDbInitializer, DbInitializer>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        
        // Configure Kafka options
        services.Configure<KafkaOptions>(
            configuration.GetSection(KafkaOptions.Section));

        // Register reservation store as singleton (in-memory state)
        services.AddSingleton<ReservationStore>();
        
        // Register reservation validation service
        services.AddSingleton<IReservationValidationService>(provider => 
            provider.GetRequiredService<ReservationStore>());

        // Register Kafka consumer as hosted service
        services.AddHostedService<ReservationEventConsumer>();
        
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
                Console.WriteLine("✓ Ordering DB initialized");
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

        return app;
    }
}