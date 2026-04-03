using Ordering.Domain.Entities;

namespace Ordering.Application.Ports;

/// <summary>
/// Repository port for Order aggregate operations.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Gets an order by its ID.
    /// </summary>
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a draft order for a user or guest token.
    /// </summary>
    Task<Order?> GetDraftOrderAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new order.
    /// </summary>
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders for a user.
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders for a guest token.
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByGuestTokenAsync(string guestToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the active (draft or pending) order that contains an item for the given seat.
    /// Used to cancel orders when a seat reservation expires.
    /// </summary>
    Task<Order?> GetActiveOrderBySeatIdAsync(Guid seatId, CancellationToken cancellationToken = default);
}