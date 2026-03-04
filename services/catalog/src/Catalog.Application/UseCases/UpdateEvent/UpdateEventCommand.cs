using MediatR;

namespace Catalog.Application.UseCases.UpdateEvent;

public record UpdateEventCommand(
    Guid EventId,
    string Name,
    string Description,
    int MaxCapacity) : IRequest<UpdateEventResponse>;