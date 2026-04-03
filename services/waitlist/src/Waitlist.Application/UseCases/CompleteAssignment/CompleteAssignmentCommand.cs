using MediatR;

namespace Waitlist.Application.UseCases.CompleteAssignment;

public record CompleteAssignmentCommand(Guid OrderId) : IRequest;
