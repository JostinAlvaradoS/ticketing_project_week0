using System.Text.Json;
using Microsoft.Extensions.Options;
using PaymentService.Application.Dtos;
using PaymentService.Application.Ports.Inbound;

namespace PaymentService.Infrastructure.Messaging;

public class PaymentApprovedEventHandler : IPaymentEventHandler
{
    private readonly IProcessPaymentApprovedUseCase _useCase;
    private readonly RabbitMQSettings _settings;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentApprovedEventHandler(
        IProcessPaymentApprovedUseCase useCase,
        IOptions<RabbitMQSettings> settings)
    {
        _useCase = useCase;
        _settings = settings.Value;
    }

    public string QueueName => _settings.ApprovedQueueName;

    public async Task<ValidationResult> HandleAsync(string json, CancellationToken cancellationToken = default)
    {
        var evt = JsonSerializer.Deserialize<PaymentApprovedEventDto>(json, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure("Invalid JSON for PaymentApprovedEvent");

        return await _useCase.ExecuteAsync(evt);
    }
}
