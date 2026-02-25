namespace Payment.Application.Ports;

/// <summary>
/// Port for validating order state and retrieving order details.
/// </summary>
public interface IOrderValidationService
{
    /// <summary>
    /// Validates that the order exists, is in pending state, and matches the expected amount.
    /// </summary>
    /// <param name="orderId">Order ID to validate</param>
    /// <param name="customerId">Customer ID that should own the order</param>
    /// <param name="expectedAmount">Expected order total amount</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with order details if valid</returns>
    Task<OrderValidationResult> ValidateOrderAsync(Guid orderId, Guid customerId, decimal expectedAmount, CancellationToken cancellationToken = default);
}

public record OrderValidationResult(
    bool IsValid,
    string? ErrorMessage,
    Guid? OrderId = null,
    string? OrderState = null,
    decimal? TotalAmount = null
);