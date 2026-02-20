using System.Text.Json;
using PaymentService.Application.Dtos;
using PaymentService.Application.Ports.Inbound;

namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// Strategy implementation for handling rejected payment events.
/// </summary>
public class PaymentRejectedEventHandler : IPaymentEventStrategy
{
    private readonly IProcessPaymentRejectedUseCase _useCase;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentRejectedEventHandler(IProcessPaymentRejectedUseCase useCase)
    {
        _useCase = useCase;
    }

    public string EventType => PaymentEventTypes.Rejected;

    public async Task<ValidationResult> HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentRejectedEventDto>(payload, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentRejectedEvent");

        return await _useCase.ExecuteAsync(evt);
    }
}
