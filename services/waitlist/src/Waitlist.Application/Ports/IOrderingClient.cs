namespace Waitlist.Application.Ports;

public interface IOrderingClient
{
    Task<Guid> CreateWaitlistOrderAsync(string email, Guid seatId, Guid eventId, CancellationToken cancellationToken = default);
    Task CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}
