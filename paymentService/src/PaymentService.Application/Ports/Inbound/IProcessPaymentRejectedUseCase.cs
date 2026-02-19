using PaymentService.Application.Dtos;

namespace PaymentService.Application.Ports.Inbound;

public interface IProcessPaymentRejectedUseCase
{
    Task<ValidationResult> ExecuteAsync(PaymentRejectedEventDto paymentEvent);
}
