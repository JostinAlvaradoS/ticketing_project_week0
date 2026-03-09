using Catalog.Application.Ports;
using MediatR;

namespace Catalog.Application.UseCases.GetEventSeatmap;

public sealed class GetEventSeatmapHandler : IRequestHandler<GetEventSeatmapQuery, GetEventSeatmapResponse?>
{
    private readonly ICatalogRepository _repository;

    public GetEventSeatmapHandler(ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetEventSeatmapResponse?> Handle(GetEventSeatmapQuery request, CancellationToken cancellationToken)
    {
        var eventEntity = await _repository.GetEventWithSeatsAsync(request.EventId, cancellationToken);

        if (eventEntity is null)
        {
            return null;
        }

        var seats = eventEntity.Seats
            .Select(s => new SeatDto(
                s.Id,
                s.SectionCode,
                s.RowNumber,
                s.SeatNumber,
                s.Price,
                s.Status
            ))
            .OrderBy(s => s.SectionCode)
            .ThenBy(s => s.RowNumber)
            .ThenBy(s => s.SeatNumber);

        return new GetEventSeatmapResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.BasePrice,
            seats
        );
    }
}