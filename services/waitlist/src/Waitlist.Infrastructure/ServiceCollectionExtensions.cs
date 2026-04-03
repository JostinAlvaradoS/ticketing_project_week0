using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Waitlist.Application.Ports;
using Waitlist.Application.UseCases.JoinWaitlist;
using Waitlist.Infrastructure.Clients;
using Waitlist.Infrastructure.Consumers;
using Waitlist.Infrastructure.Options;
using Waitlist.Infrastructure.Persistence;
using Waitlist.Infrastructure.Workers;

namespace Waitlist.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(JoinWaitlistHandler).Assembly));

        services.AddDbContext<WaitlistDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "bc_waitlist")));

        services.AddScoped<IWaitlistRepository, WaitlistRepository>();

        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.Section));

        // HTTP clients (adapters for ports)
        services.AddHttpClient<ICatalogClient, CatalogHttpClient>(c =>
            c.BaseAddress = new Uri(configuration["Services:CatalogUrl"] ?? "http://catalog-service:5001"));

        services.AddHttpClient<IOrderingClient, OrderingHttpClient>(c =>
            c.BaseAddress = new Uri(configuration["Services:OrderingUrl"] ?? "http://ordering-service:5002"));

        services.AddHttpClient<IInventoryClient, InventoryHttpClient>(c =>
            c.BaseAddress = new Uri(configuration["Services:InventoryUrl"] ?? "http://inventory-service:5003"));

        services.AddScoped<IEmailService, SmtpEmailService>();

        // Background services
        services.AddHostedService<ReservationExpiredConsumer>();
        services.AddHostedService<PaymentSucceededConsumer>();
        services.AddHostedService<WaitlistExpiryWorker>();

        return services;
    }

    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<WaitlistDbContext>();
                db.Database.Migrate();
                Console.WriteLine("✅ Waitlist migrations applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning: Could not apply migrations: {ex.Message}");
            }
        }

        app.UseRouting();
        app.MapControllers();
        return app;
    }
}
