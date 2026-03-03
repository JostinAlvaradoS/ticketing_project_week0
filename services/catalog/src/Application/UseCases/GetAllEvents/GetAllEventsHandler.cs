using Catalog.Application.Ports;
using MediatR;

namespace Catalog.Application.UseCases.GetAllEvents;

public sealed class GetAllEventsHandler : IRequestHandler<GetAllEventsQuery, IEnumerable<EventDto>>
{
    private readonly ICatalogRepository _repository;

    public GetAllEventsHandler(ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<EventDto>> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await _repository.GetAllEventsWithSeatsAsync(cancellationToken);

        return events
            .Select(e => {
                var soldSeats = e.GetSoldSeatsCount();
                var revenue = e.Seats.Where(s => s.IsSold()).Sum(s => s.Price);
                return new EventDto(
                    e.Id,
                    e.Name,
                    e.Description,
                    e.EventDate,
                    e.Venue,
                    e.MaxCapacity,
                    e.BasePrice,
                    e.IsActive,
                    e.Seats.Count,
                    soldSeats,
                    revenue
                );
            })
            .OrderByDescending(e => e.EventDate);
    }
}
