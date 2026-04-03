using MediatR;

namespace Waitlist.Application.UseCases.AssignNext;

public record AssignNextCommand(Guid EventId, Guid SeatId) : IRequest;
