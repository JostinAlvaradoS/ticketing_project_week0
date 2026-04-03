namespace Waitlist.Application.Ports;

public interface IInventoryClient
{
    Task ReleaseSeatAsync(Guid seatId, CancellationToken cancellationToken = default);
}
