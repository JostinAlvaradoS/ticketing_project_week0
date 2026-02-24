using MediatR;

namespace Catalog.Application.UseCases.GetEvent;

public record GetEventQuery(Guid EventId) : IRequest<GetEventResponse?>;
