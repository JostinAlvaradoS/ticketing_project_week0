using System.ComponentModel.DataAnnotations;

namespace Ordering.Domain.Entities;

public class Order
{
    public const string StateDraft = "draft";
    public const string StatePending = "pending";
    public const string StatePaid = "paid";
    public const string StateFulfilled = "fulfilled";
    public const string StateCancelled = "cancelled";

    public Guid Id { get; private set; }
    public string? UserId { get; private set; }
    public string? GuestToken { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string State { get; private set; } = StateDraft;
    public DateTime CreatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Constructor privado — EF Core lo usa mediante reflexión
    private Order() { }

    /// <summary>
    /// Crea un nuevo pedido en estado Draft.
    /// </summary>
    public static Order Create(string? userId, string? guestToken)
    {
        if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(guestToken))
            throw new ArgumentException("Either UserId or GuestToken must be provided.");

        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GuestToken = guestToken,
            State = StateDraft,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Agrega un ítem al pedido y recalcula el total. Solo válido en estado Draft.
    /// </summary>
    public OrderItem AddItem(Guid seatId, decimal price)
    {
        if (State != StateDraft)
            throw new InvalidOperationException($"Cannot add items to an order in state '{State}'.");

        if (_items.Any(i => i.SeatId == seatId))
            throw new InvalidOperationException("Seat is already in the cart.");

        var item = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = Id,
            SeatId = seatId,
            Price = price
        };

        _items.Add(item);
        TotalAmount = _items.Sum(i => i.Price);
        return item;
    }

    /// <summary>
    /// Transiciona el pedido de Draft a Pending (listo para pago).
    /// </summary>
    public void Checkout()
    {
        if (State != StateDraft)
            throw new InvalidOperationException($"Cannot checkout an order in state '{State}'. Expected: '{StateDraft}'.");

        if (!_items.Any())
            throw new InvalidOperationException("Order is empty. Add at least one item before checkout.");

        State = StatePending;
    }

    /// <summary>
    /// Transiciona el pedido de Pending a Paid.
    /// </summary>
    public void MarkAsPaid()
    {
        if (State != StatePending)
            throw new InvalidOperationException($"Cannot mark as paid an order in state '{State}'. Expected: '{StatePending}'.");

        State = StatePaid;
        PaidAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Cancela el pedido si está en Draft o Pending.
    /// </summary>
    public void Cancel()
    {
        if (State == StatePaid || State == StateFulfilled)
            throw new InvalidOperationException($"Cannot cancel an order in state '{State}'.");

        State = StateCancelled;
    }

    /// <summary>
    /// Verifica si el pedido pertenece al usuario o token de invitado dado.
    /// </summary>
    public bool BelongsTo(string? userId, string? guestToken)
    {
        if (!string.IsNullOrEmpty(userId))
            return UserId == userId;
        if (!string.IsNullOrEmpty(guestToken))
            return GuestToken == guestToken;
        return false;
    }
}

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid SeatId { get; set; }
    public decimal Price { get; set; }
    public Order Order { get; set; } = null!;
}
