namespace Inventory.Domain.Entities;

/// <summary>
/// Representa un asiento reservado con TTL. Una vez expirado, un worker en background maneja la limpieza.
/// </summary>
public class Reservation
{
    public const string StatusActive = "active";
    public const string StatusExpired = "expired";
    public const string StatusConfirmed = "confirmed";

    public Guid Id { get; private set; }
    public Guid SeatId { get; private set; }
    public Guid EventId { get; private set; }
    public string CustomerId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string Status { get; set; } = StatusActive; // set accesible para EF Core y el worker de expiración

    // Constructor sin parámetros requerido por EF Core
    private Reservation() { }

    /// <summary>
    /// Crea una reserva válida con TTL configurable.
    /// </summary>
    public static Reservation Create(Guid seatId, string customerId, Guid eventId = default, int ttlMinutes = 1)
    {
        if (seatId == Guid.Empty) throw new ArgumentException("SeatId cannot be empty.", nameof(seatId));
        if (string.IsNullOrWhiteSpace(customerId)) throw new ArgumentException("CustomerId cannot be empty.", nameof(customerId));
        if (ttlMinutes <= 0) throw new ArgumentException("TTL must be positive.", nameof(ttlMinutes));

        var now = DateTime.UtcNow;
        return new Reservation
        {
            Id = Guid.NewGuid(),
            SeatId = seatId,
            EventId = eventId,
            CustomerId = customerId,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(ttlMinutes),
            Status = StatusActive
        };
    }

    /// <summary>
    /// Verifica si la reserva ha superado su TTL.
    /// </summary>
    public bool IsExpired(DateTime currentTime) =>
        Status == StatusExpired || currentTime > ExpiresAt;
}
