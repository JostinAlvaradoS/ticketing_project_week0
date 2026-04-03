namespace Waitlist.Application.Ports;

public interface ICatalogClient
{
    Task<int> GetAvailableCountAsync(Guid eventId, CancellationToken cancellationToken = default);
}
