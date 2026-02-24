using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ordering.Application.Ports;
using Ordering.Infrastructure.Events;
using Ordering.Infrastructure.Persistence;

namespace Ordering.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
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
}