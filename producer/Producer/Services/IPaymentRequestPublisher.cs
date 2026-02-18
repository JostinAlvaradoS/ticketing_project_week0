using Producer.Models;

namespace Producer.Services;

public interface IPaymentRequestPublisher
{
    Task PublishPaymentRequestedAsync(PaymentRequestedEvent paymentEvent, CancellationToken cancellationToken = default);
}
