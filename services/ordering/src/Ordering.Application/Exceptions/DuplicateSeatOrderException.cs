namespace Ordering.Application.Exceptions;

/// <summary>
/// Thrown when an active order for the same seat already exists.
/// Mapped to HTTP 409 Conflict by the controller.
/// </summary>
public class DuplicateSeatOrderException : Exception
{
    public Guid SeatId { get; }

    public DuplicateSeatOrderException(Guid seatId)
        : base($"An active order for seat {seatId} already exists.")
    {
        SeatId = seatId;
    }
}
