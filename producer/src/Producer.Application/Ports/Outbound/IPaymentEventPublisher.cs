using Producer.Domain.Events;

namespace Producer.Application.Ports.Outbound;

public interface IPaymentEventPublisher
{
    Task PublishApprovedAsync(PaymentApprovedEvent paymentEvent, CancellationToken cancellationToken = default);
    Task PublishRejectedAsync(PaymentRejectedEvent paymentEvent, CancellationToken cancellationToken = default);
}
