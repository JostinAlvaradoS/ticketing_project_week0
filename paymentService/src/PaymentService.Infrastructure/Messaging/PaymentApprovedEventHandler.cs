using System.Text.Json;
using PaymentService.Application.Dtos;
using PaymentService.Application.Ports.Inbound;

namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// Strategy implementation for handling approved payment events.
/// </summary>
public class PaymentApprovedEventHandler : IPaymentEventStrategy
{
    private readonly IProcessPaymentApprovedUseCase _useCase;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentApprovedEventHandler(IProcessPaymentApprovedUseCase useCase)
    {
        _useCase = useCase;
    }

    public string EventType => PaymentEventTypes.Approved;

    public async Task<ValidationResult> HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentApprovedEventDto>(payload, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentApprovedEvent");

        return await _useCase.ExecuteAsync(evt);
    }
}
