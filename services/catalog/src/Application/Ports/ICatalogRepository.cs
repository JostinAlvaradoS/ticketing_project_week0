using Catalog.Domain.Entities;

namespace Catalog.Application.Ports;

public interface ICatalogRepository
{
    Task<IEnumerable<Event>> GetAllEventsAsync(CancellationToken cancellationToken = default);

    Task<Event?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<Event?> GetEventWithSeatsAsync(Guid eventId, CancellationToken cancellationToken = default);
}