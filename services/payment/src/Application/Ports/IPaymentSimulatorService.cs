namespace Payment.Application.Ports;

/// <summary>
/// Port for simulating payment processing.
/// </summary>
public interface IPaymentSimulatorService
{
    /// <summary>
    /// Simulates payment processing for the given amount and payment method.
    /// </summary>
    /// <param name="amount">Payment amount</param>
    /// <param name="currency">Payment currency</param>
    /// <param name="paymentMethod">Payment method</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Simulation result</returns>
    Task<PaymentSimulationResult> SimulatePaymentAsync(decimal amount, string currency, string paymentMethod, CancellationToken cancellationToken = default);
}

public record PaymentSimulationResult(
    bool Success,
    string? TransactionId,
    string? ErrorCode,
    string? ErrorMessage,
    string? FailureReason
);