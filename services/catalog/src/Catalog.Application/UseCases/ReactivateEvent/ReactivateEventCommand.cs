using MediatR;

namespace Catalog.Application.UseCases.ReactivateEvent;

public record ReactivateEventCommand(Guid EventId) : IRequest<ReactivateEventResponse>;