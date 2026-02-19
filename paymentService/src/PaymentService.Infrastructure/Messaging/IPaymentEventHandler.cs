using PaymentService.Application.Dtos;

namespace PaymentService.Infrastructure.Messaging;

public interface IPaymentEventHandler
{
    string QueueName { get; }
    Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default);
}
