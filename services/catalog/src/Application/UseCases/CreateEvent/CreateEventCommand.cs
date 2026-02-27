using MediatR;

namespace Catalog.Application.UseCases.CreateEvent;

public record CreateEventCommand(
    string Name,
    string Description,
    DateTime EventDate,
    string Venue,
    int MaxCapacity,
    decimal BasePrice) : IRequest<CreateEventResponse>;