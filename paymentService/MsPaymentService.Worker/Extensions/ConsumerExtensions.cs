using Microsoft.Extensions.DependencyInjection;
using MsPaymentService.Worker.Messaging;

namespace MsPaymentService.Worker.Extensions;

public static class ConsumerExtensions
{
    public static IServiceCollection AddTicketPaymentConsumer(
        this IServiceCollection services)
    {
        services.AddSingleton<TicketPaymentConsumer>();
        return services;
    }
}
