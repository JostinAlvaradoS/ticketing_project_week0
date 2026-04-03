namespace Waitlist.Application.Ports;

public interface IOrderingClient
{
    Task<Guid> CreateWaitlistOrderAsync(Guid seatId, decimal price, string guestToken, Guid concertEventId, CancellationToken cancellationToken = default);
    Task CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}
