using MediatR;

namespace Waitlist.Application.UseCases.AssignNext;

public record AssignNextCommand(Guid SeatId, Guid ConcertEventId) : IRequest;
