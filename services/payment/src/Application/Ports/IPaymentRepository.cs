namespace Payment.Application.Ports;

/// <summary>
/// Port for payment data persistence.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Creates a new payment record.
    /// </summary>
    /// <param name="payment">Payment entity to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created payment entity</returns>
    Task<Domain.Entities.Payment> CreateAsync(Domain.Entities.Payment payment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing payment record.
    /// </summary>
    /// <param name="payment">Payment entity to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated payment entity</returns>
    Task<Domain.Entities.Payment> UpdateAsync(Domain.Entities.Payment payment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a payment by ID.
    /// </summary>
    /// <param name="paymentId">Payment ID to retrieve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment entity if found, null otherwise</returns>
    Task<Domain.Entities.Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves payments by order ID.
    /// </summary>
    /// <param name="orderId">Order ID to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of payments for the order</returns>
    Task<IEnumerable<Domain.Entities.Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}