using FluentValidation;
using MediatR;

namespace Waitlist.Application.UseCases.JoinWaitlist;

public record JoinWaitlistCommand(string Email, Guid EventId) : IRequest<JoinWaitlistResult>;

public record JoinWaitlistResult(Guid EntryId, int Position);

public class JoinWaitlistCommandValidator : AbstractValidator<JoinWaitlistCommand>
{
    public JoinWaitlistCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("EventId is required.");
    }
}
