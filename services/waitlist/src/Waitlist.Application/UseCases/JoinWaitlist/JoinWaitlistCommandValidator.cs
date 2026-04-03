using FluentValidation;

namespace Waitlist.Application.UseCases.JoinWaitlist;

public class JoinWaitlistCommandValidator : AbstractValidator<JoinWaitlistCommand>
{
    public JoinWaitlistCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.EventId)
            .NotEmpty().WithMessage("EventId is required.");
    }
}
