using FluentAssertions;
using Waitlist.Application.UseCases.JoinWaitlist;

namespace Waitlist.UnitTests.Application;

public class JoinWaitlistCommandValidatorTests
{
    private readonly JoinWaitlistCommandValidator _validator = new();

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@nodomain")]
    [InlineData("")]
    public async Task Validator_InvalidEmail_HasValidationError(string email)
    {
        var result = await _validator.ValidateAsync(new JoinWaitlistCommand(email, Guid.NewGuid()));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Validator_ValidEmail_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new JoinWaitlistCommand("user@example.com", Guid.NewGuid()));
        result.Errors.Should().NotContain(e => e.PropertyName == "Email");
    }
}
