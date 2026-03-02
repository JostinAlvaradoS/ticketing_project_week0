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
        var events = await _repository.GetAllEventsAsync(cancellationToken);

        return events
            .Select(e => new EventDto(
                e.Id,
                e.Name,
                e.Description,
                e.EventDate,
                e.Venue,
                e.MaxCapacity,
                e.BasePrice,
                e.IsActive
            ))
            .OrderByDescending(e => e.EventDate);
    }
}
