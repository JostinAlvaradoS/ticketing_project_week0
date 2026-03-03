using MediatR;

namespace Catalog.Application.UseCases.DeactivateEvent;

public record DeactivateEventCommand(Guid EventId) : IRequest<DeactivateEventResponse>;