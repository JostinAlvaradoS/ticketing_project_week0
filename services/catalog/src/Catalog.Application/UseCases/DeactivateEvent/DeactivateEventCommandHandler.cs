using Catalog.Application.Ports;
using MediatR;

namespace Catalog.Application.UseCases.DeactivateEvent;

public class DeactivateEventCommandHandler : IRequestHandler<DeactivateEventCommand, DeactivateEventResponse>
{
    private readonly ICatalogRepository _catalogRepository;

    public DeactivateEventCommandHandler(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<DeactivateEventResponse> Handle(DeactivateEventCommand request, CancellationToken cancellationToken)
    {
        // Get event with seats to validate business rules (T106)
        var eventEntity = await _catalogRepository.GetEventWithSeatsAsync(request.EventId, cancellationToken);
        
        if (eventEntity == null)
            throw new KeyNotFoundException($"Event with ID {request.EventId} not found");

        // Deactivate event using domain logic - will throw if active reservations exist
        eventEntity.Deactivate();

        // Save changes
        await _catalogRepository.SaveChangesAsync(cancellationToken);

        // Return response matching Gherkin expectations
        return new DeactivateEventResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.Status,
            eventEntity.UpdatedAt,
            true);
    }
}