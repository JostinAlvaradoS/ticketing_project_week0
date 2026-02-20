using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Ports.Inbound;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Application.UseCases;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddPaymentServiceInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> dbOptions,
        Action<RabbitMQSettings> rabbitMqOptions)
    {
        // Persistence
        services.AddDbContext<PaymentDbContext>(dbOptions);
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Use Cases
        services.AddScoped<IProcessPaymentApprovedUseCase, ProcessPaymentApprovedUseCase>();
        services.AddScoped<IProcessPaymentRejectedUseCase, ProcessPaymentRejectedUseCase>();

        // Messaging — Strategy pattern: each handler is registered as IPaymentEventStrategy.
        // To add a new event type: create a new IPaymentEventStrategy implementation,
        // register it here, and add the queue mapping in RabbitMQSettings.
        services.Configure(rabbitMqOptions);
        services.AddScoped<IPaymentEventStrategy, PaymentApprovedEventHandler>();
        services.AddScoped<IPaymentEventStrategy, PaymentRejectedEventHandler>();
        services.AddSingleton<PaymentEventStrategyResolver>();
        services.AddHostedService<TicketPaymentConsumer>();

        return services;
    }
}
