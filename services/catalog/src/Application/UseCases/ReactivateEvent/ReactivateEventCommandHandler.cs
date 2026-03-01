using Catalog.Application.Ports;
using MediatR;

namespace Catalog.Application.UseCases.ReactivateEvent;

public class ReactivateEventCommandHandler : IRequestHandler<ReactivateEventCommand, ReactivateEventResponse>
{
    private readonly ICatalogRepository _catalogRepository;

    public ReactivateEventCommandHandler(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<ReactivateEventResponse> Handle(ReactivateEventCommand request, CancellationToken cancellationToken)
    {
        // Get event with seats to validate business rules (T106)
        var eventEntity = await _catalogRepository.GetEventWithSeatsAsync(request.EventId, cancellationToken);
        
        if (eventEntity == null)
            throw new KeyNotFoundException($"Event with ID {request.EventId} not found");

        // Reactivate event using domain logic - will throw if event date is past
        eventEntity.Reactivate();

        // Save changes
        await _catalogRepository.SaveChangesAsync(cancellationToken);

        // Return response matching Gherkin expectations
        return new ReactivateEventResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.Status,
            eventEntity.UpdatedAt,
            true);
    }
}