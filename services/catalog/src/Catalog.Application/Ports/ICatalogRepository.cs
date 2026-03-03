using Catalog.Domain.Entities;

namespace Catalog.Application.Ports;

public interface ICatalogRepository
{
    Task<IEnumerable<Event>> GetAllEventsAsync(CancellationToken cancellationToken = default);

    Task<Event?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<Event?> GetEventWithSeatsAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task UpdateSeatStatusAsync(Guid seatId, string status, CancellationToken cancellationToken = default);

    Task UpdateSeatStatusAsync(Guid seatId, string status, Guid? reservationId, CancellationToken cancellationToken = default);

    Task UpdateSeatStatusByReservationAsync(Guid reservationId, string status, CancellationToken cancellationToken = default);
}