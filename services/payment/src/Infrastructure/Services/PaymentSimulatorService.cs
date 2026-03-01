using Microsoft.Extensions.Logging;
using Payment.Application.Ports;

namespace Payment.Infrastructure.Services;

public class PaymentSimulatorService : IPaymentSimulatorService
{
    private readonly ILogger<PaymentSimulatorService> _logger;
    private readonly Random _random = new();

    public PaymentSimulatorService(ILogger<PaymentSimulatorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaymentSimulationResult> SimulatePaymentAsync(decimal amount, string currency, string paymentMethod, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Simulating payment: {Amount} {Currency} via {PaymentMethod}", amount, currency, paymentMethod);

        // Simulate processing delay
        await Task.Delay(_random.Next(100, 500), cancellationToken);

        // Simulate payment scenarios based on amount
        var result = amount switch
        {
            // Special test amounts for different scenarios
            < 0 => SimulateFailure("INVALID_AMOUNT", "Amount cannot be negative", "invalid_amount"),
            0 => SimulateFailure("ZERO_AMOUNT", "Amount cannot be zero", "invalid_amount"),
            >= 999999.99m => SimulateFailure("AMOUNT_TOO_LARGE", "Amount exceeds maximum limit", "processor_error"),
            
            // Simulate card declined for amounts ending in .13. No limit check anymore!
            var amt when amt % 1 == 0.13m => SimulateFailure("CARD_DECLINED", "Your card was declined", "card_declined"),
            
            // Simulate insufficient funds for amounts ending in .66
            var amt when amt % 1 == 0.66m => SimulateFailure("INSUFFICIENT_FUNDS", "Insufficient funds on card", "insufficient_funds"),
            
            // Simulate processor timeout for amounts ending in .99
            var amt when amt % 1 == 0.99m => SimulateFailure("PROCESSOR_TIMEOUT", "Payment processor timeout", "timeout"),
            
            // Default success case
            _ => SimulateSuccess()
        };

        _logger.LogDebug("Payment simulation result: {Success}, TransactionId: {TransactionId}, Error: {Error}", 
            result.Success, result.TransactionId, result.ErrorMessage);

        return result;
    }

    private PaymentSimulationResult SimulateSuccess()
    {
        // Generate a mock transaction ID
        var transactionId = $"txn_{Guid.NewGuid().ToString("N")[0..16]}";
        return new PaymentSimulationResult(true, transactionId, null, null, null);
    }

    private static PaymentSimulationResult SimulateFailure(string errorCode, string errorMessage, string failureReason)
    {
        return new PaymentSimulationResult(false, null, errorCode, errorMessage, failureReason);
    }
}