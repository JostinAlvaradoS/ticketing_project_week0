using MediatR;

namespace Waitlist.Application.UseCases.JoinWaitlist;

public record JoinWaitlistCommand(string Email, Guid EventId) : IRequest<JoinWaitlistResult>;

public record JoinWaitlistResult(
    Guid WaitlistEntryId,
    int Position,
    string Email,
    Guid EventId
);
