using Payment.Domain.Entities;
using FluentAssertions;

namespace Payment.Domain.UnitTests;

public class PaymentTests
{
    [Fact]
    public void IsValidForProcess_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        var result = payment.IsValidForProcess();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void IsValidForProcess_WithInvalidAmount_ShouldReturnFalse(decimal invalidAmount)
    {
        // Arrange
        var payment = CreateValidPayment();
        payment.Amount = invalidAmount;

        // Act
        var result = payment.IsValidForProcess();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidForProcess_WithInvalidPaymentMethod_ShouldReturnFalse(string invalidMethod)
    {
        // Arrange
        var payment = CreateValidPayment();
        payment.PaymentMethod = invalidMethod;

        // Act
        var result = payment.IsValidForProcess();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MarkAsSucceeded_FromPending_ShouldChangeStatusAndSetProcessedAt()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkAsSucceeded();

        // Assert
        payment.Status.Should().Be(Payment.Entities.Payment.StatusSucceeded);
        payment.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsSucceeded_FromNonPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment = CreateValidPayment();
        payment.MarkAsSucceeded();

        // Act & Assert
        var action = () => payment.MarkAsSucceeded();
        action.Should().Throw<InvalidOperationException>()
              .WithMessage("Cannot succeed payment from status: succeeded");
    }

    [Fact]
    public void MarkAsFailed_FromPending_ShouldChangeStatusSetErrorsAndSetProcessedAt()
    {
        // Arrange
        var payment = CreateValidPayment();

        // Act
        payment.MarkAsFailed("ERR01", "Invalid card");

        // Assert
        payment.Status.Should().Be(Payment.Entities.Payment.StatusFailed);
        payment.ErrorCode.Should().Be("ERR01");
        payment.ErrorMessage.Should().Be("Invalid card");
        payment.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsFailed_FromNonPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var payment = CreateValidPayment();
        payment.MarkAsSucceeded();

        // Act & Assert
        var action = () => payment.MarkAsFailed("ERR02", "Error");
        action.Should().Throw<InvalidOperationException>()
              .WithMessage("Cannot fail payment from status: succeeded");
    }

    // Helper method
    private static Payment.Entities.Payment CreateValidPayment()
    {
        return new Payment.Entities.Payment
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Amount = 100.00m,
            Currency = "USD",
            PaymentMethod = "CreditCard",
            Status = Payment.Entities.Payment.StatusPending,
            CreatedAt = DateTime.UtcNow
        };
    }
}