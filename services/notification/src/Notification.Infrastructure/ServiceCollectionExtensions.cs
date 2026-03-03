using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notification.Application.Ports;
using Notification.Infrastructure.Email;
using Notification.Infrastructure.Events;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Controllers
        services.AddControllers();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Notification.Application.UseCases.SendTicketNotification.SendTicketNotificationHandler).Assembly));

        // Add Database
        services.AddDbContext<NotificationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"),
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_notification"));
        });

        services.AddScoped<IDbInitializer, DbInitializer>();
        services.AddScoped<IEmailNotificationRepository, EmailNotificationRepository>();

        // Add Email Service
        services.Configure<SmtpEmailOptions>(
            configuration.GetSection(SmtpEmailOptions.Section));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Add Kafka Event Consumer
        services.Configure<KafkaOptions>(
            configuration.GetSection(KafkaOptions.Section));
        services.AddHostedService<TicketIssuedEventConsumer>();

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
                Console.WriteLine("✓ Notification DB initialized");
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

        app.UseRouting();
        app.MapControllers();

        return app;
    }
}
