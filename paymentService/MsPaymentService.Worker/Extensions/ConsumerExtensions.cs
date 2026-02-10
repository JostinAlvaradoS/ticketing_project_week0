using Microsoft.Extensions.DependencyInjection;
using PaymentService.Api.Messaging.RabbitMQ;

namespace PaymentService.Api.Extensions;

public static class ConsumerExtensions
{
    public static IServiceCollection AddTicketPaymentConsumer(
        this IServiceCollection services)
    {
        services.AddSingleton<TicketPaymentConsumer>();
        return services;
    }
}
