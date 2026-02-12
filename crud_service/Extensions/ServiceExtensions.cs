using CrudService.Data;
using CrudService.Models.Entities;
using CrudService.Repositories;
using CrudService.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrudService.Extensions;

/// <summary>
/// Extensiones para registrar servicios
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Registra todos los servicios y repositorios
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // HUMAN CHECK:
        // La IA sugirio crear DbContext como Transient (nueva instancia por request)
        // eso era demasiado ineficiente porque iba a hacer una saturacion de conexiones en la base,
        // asi que lo cambiamos a Scoped.
        
        // CORRECCION CRIT-002: Mapear ENUMs de PostgreSQL correctamente
        // El codigo original usaba HasConversion<string>() que enviaba texto,
        // pero PostgreSQL tiene tipos ENUM nativos (ticket_status, payment_status)
        // que requieren este mapeo explicito.
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<TicketStatus>("ticket_status");
        dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<TicketingDbContext>(options =>
            options.UseNpgsql(dataSource));

        // Repositorios (Scoped: viven en ciclo del request)
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();

        // Servicios (Scoped: dependen de repositorios)
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITicketService, TicketService>();

        return services;
    }
}
