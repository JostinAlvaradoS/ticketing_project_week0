using Catalog.Application.Ports;
using MediatR;

namespace Catalog.Application.UseCases.GetEvent;

public sealed class GetEventHandler : IRequestHandler<GetEventQuery, GetEventResponse?>
{
    private readonly ICatalogRepository _repository;

    public GetEventHandler(ICatalogRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetEventResponse?> Handle(GetEventQuery request, CancellationToken cancellationToken)
    {
        var eventEntity = await _repository.GetEventWithSeatsAsync(request.EventId, cancellationToken);

        if (eventEntity is null)
        {
            return null;
        }

        var soldSeats = eventEntity.GetSoldSeatsCount();
        var soldSeatsList = eventEntity.Seats.Where(s => s.IsSold());
        var revenue = soldSeatsList.Sum(s => s.Price);

        return new GetEventResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Venue,
            eventEntity.MaxCapacity,
            eventEntity.BasePrice,
            eventEntity.Status == "active",
            eventEntity.Seats.Count,
            eventEntity.GetAvailableSeatsCount(),
            eventEntity.GetReservedSeatsCount(),
            soldSeats,
            revenue
        );
    }
}
