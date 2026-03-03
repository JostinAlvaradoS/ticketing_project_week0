using Catalog.Application.Ports;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.UseCases.CreateEvent;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, CreateEventResponse>
{
    private readonly ICatalogRepository _catalogRepository;

    public CreateEventCommandHandler(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<CreateEventResponse> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // Create Event using factory method with validation (from T101)
        var eventEntity = Event.Create(
            request.Name,
            request.Description,
            request.EventDate,
            request.Venue,
            request.MaxCapacity,
            request.BasePrice);

        // Persist the event
        var createdEvent = await _catalogRepository.CreateEventAsync(eventEntity, cancellationToken);
        await _catalogRepository.SaveChangesAsync(cancellationToken);

        // Return response matching Gherkin expectations
        return new CreateEventResponse(
            createdEvent.Id,
            createdEvent.Name,
            createdEvent.Description,
            createdEvent.EventDate,
            createdEvent.Venue,
            createdEvent.MaxCapacity,
            createdEvent.BasePrice,
            createdEvent.Status,
            createdEvent.CreatedAt);
    }
}