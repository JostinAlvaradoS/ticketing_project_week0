using PaymentService.Application.Dtos;

namespace PaymentService.Application.Ports.Inbound;

public interface IProcessPaymentApprovedUseCase
{
    Task<ValidationResult> ExecuteAsync(PaymentApprovedEventDto paymentEvent);
}
