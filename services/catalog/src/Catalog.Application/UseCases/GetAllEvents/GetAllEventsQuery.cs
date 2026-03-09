using MediatR;

namespace Catalog.Application.UseCases.GetAllEvents;

public record GetAllEventsQuery : IRequest<IEnumerable<EventDto>>;
