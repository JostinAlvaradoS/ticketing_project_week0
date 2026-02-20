using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReservationService.Application.Ports.Inbound;
using ReservationService.Application.Ports.Outbound;
using ReservationService.Application.UseCases;
using ReservationService.Infrastructure.Messaging;
using ReservationService.Infrastructure.Persistence;

namespace ReservationService.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddReservationServiceInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbOptions,
        Action<RabbitMQSettings> rabbitMqOptions)
    {
        services.AddDbContext<TicketingDbContext>(dbOptions);

        services.Configure(rabbitMqOptions);

        services.AddScoped<ITicketRepository, TicketRepository>();

        services.AddScoped<IReserveTicketUseCase, ReserveTicketUseCase>();

        services.AddHostedService<TicketReservationConsumer>();

        return services;
    }
}
