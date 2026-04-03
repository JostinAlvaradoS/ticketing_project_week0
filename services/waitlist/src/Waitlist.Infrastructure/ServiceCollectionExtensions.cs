using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Waitlist.Application.Ports;
using Waitlist.Infrastructure.Clients;
using Waitlist.Infrastructure.Consumers;
using Waitlist.Infrastructure.Persistence;
using Waitlist.Infrastructure.Services;
using Waitlist.Infrastructure.Workers;

namespace Waitlist.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<WaitlistDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "bc_waitlist")));

        // Repository
        services.AddScoped<IWaitlistRepository, WaitlistRepository>();

        // Email service
        services.AddScoped<IEmailService, EmailService>();

        var kafkaBootstrap = configuration.GetSection("Kafka")["BootstrapServers"] ?? "localhost:9092";
        var catalogUrl = configuration["Services:CatalogUrl"] ?? "http://catalog:5001";
        var orderingUrl = configuration["Services:OrderingUrl"] ?? "http://ordering:5003";
        var inventoryUrl = configuration["Services:InventoryUrl"] ?? "http://inventory:5002";

        // HTTP clients
        services.AddHttpClient<ICatalogClient, CatalogHttpClient>(c => c.BaseAddress = new Uri(catalogUrl));
        services.AddHttpClient<IOrderingClient, OrderingHttpClient>(c => c.BaseAddress = new Uri(orderingUrl));
        services.AddHttpClient<IInventoryClient, InventoryHttpClient>(c => c.BaseAddress = new Uri(inventoryUrl));

        // Expiry worker (singleton → uses scope factory internally)
        services.AddSingleton<IHostedService, WaitlistExpiryWorker>(sp =>
            new WaitlistExpiryWorker(sp.GetRequiredService<IServiceScopeFactory>()));

        // Kafka consumers
        services.AddSingleton<IHostedService, ReservationExpiredConsumer>(sp =>
            new ReservationExpiredConsumer(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ReservationExpiredConsumer>>(),
                kafkaBootstrap));

        services.AddSingleton<IHostedService, PaymentSucceededConsumer>(sp =>
            new PaymentSucceededConsumer(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<PaymentSucceededConsumer>>(),
                kafkaBootstrap));

        return services;
    }
}
