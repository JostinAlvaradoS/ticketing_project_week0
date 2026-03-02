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
        var eventEntity = await _repository.GetEventAsync(request.EventId, cancellationToken);

        if (eventEntity is null)
        {
            return null;
        }

        return new GetEventResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Venue,
            eventEntity.MaxCapacity,
            eventEntity.BasePrice,
            eventEntity.Status == "active"
        );
    }
}
