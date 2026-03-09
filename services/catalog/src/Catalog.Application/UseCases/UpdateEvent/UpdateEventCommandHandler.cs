using Catalog.Application.Ports;
using MediatR;

namespace Catalog.Application.UseCases.UpdateEvent;

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, UpdateEventResponse>
{
    private readonly ICatalogRepository _catalogRepository;

    public UpdateEventCommandHandler(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<UpdateEventResponse> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        // Get event with seats to validate business rules
        var eventEntity = await _catalogRepository.GetEventWithSeatsAsync(request.EventId, cancellationToken);
        
        if (eventEntity == null)
            throw new KeyNotFoundException($"Event with ID {request.EventId} not found");

        // Update event details using domain logic (T106)
        eventEntity.UpdateDetails(
            request.Name,
            request.Description,
            request.MaxCapacity);

        // Save changes
        await _catalogRepository.SaveChangesAsync(cancellationToken);

        // Return response matching Gherkin expectations  
        return new UpdateEventResponse(
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.Description,
            eventEntity.EventDate,
            eventEntity.Venue,
            eventEntity.MaxCapacity,
            eventEntity.BasePrice,
            eventEntity.Status,
            eventEntity.CreatedAt,
            eventEntity.UpdatedAt);
    }
}