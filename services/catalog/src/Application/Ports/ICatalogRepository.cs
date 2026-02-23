using Catalog.Domain.Entities;

namespace Catalog.Application.Ports;

public interface ICatalogRepository
{
    Task<Event?> GetEventWithSeatsAsync(Guid eventId, CancellationToken cancellationToken = default);
}