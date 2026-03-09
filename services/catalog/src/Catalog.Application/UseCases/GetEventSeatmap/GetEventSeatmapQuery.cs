using MediatR;

namespace Catalog.Application.UseCases.GetEventSeatmap;

public record GetEventSeatmapQuery(Guid EventId) : IRequest<GetEventSeatmapResponse?>;