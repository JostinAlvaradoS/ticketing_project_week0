using Catalog.Domain.Entities;
using FluentAssertions;

namespace Catalog.Domain.UnitTests.Entities;

public class SeatTests
{
    [Fact]
    public void IsValidForCreation_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var seat = CreateValidSeat();

        // Act
        var result = seat.IsValidForCreation();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidForCreation_WithInvalidSectionCode_ShouldReturnFalse(string invalidSectionCode)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.SectionCode = invalidSectionCode;

        // Act
        var result = seat.IsValidForCreation();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void IsValidForCreation_WithInvalidRowNumber_ShouldReturnFalse(int invalidRowNumber)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.RowNumber = invalidRowNumber;

        // Act
        var result = seat.IsValidForCreation();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void IsValidForCreation_WithInvalidSeatNumber_ShouldReturnFalse(int invalidSeatNumber)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.SeatNumber = invalidSeatNumber;

        // Act
        var result = seat.IsValidForCreation();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void IsValidForCreation_WithInvalidPrice_ShouldReturnFalse(decimal invalidPrice)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Price = invalidPrice;

        // Act
        var result = seat.IsValidForCreation();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("INVALID")]
    [InlineData("pending")]
    public void IsValidForCreation_WithInvalidStatus_ShouldReturnFalse(string invalidStatus)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = invalidStatus;

        // Act
        var result = seat.IsValidForCreation();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(Seat.StatusAvailable)]
    [InlineData(Seat.StatusReserved)]
    [InlineData(Seat.StatusSold)]
    public void IsValidForCreation_WithValidStatus_ShouldReturnTrue(string validStatus)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = validStatus;

        // Act
        var result = seat.IsValidForCreation();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WithAvailableStatus_ShouldReturnTrue()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusAvailable;

        // Act
        var result = seat.IsAvailable();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(Seat.StatusReserved)]
    [InlineData(Seat.StatusSold)]
    public void IsAvailable_WithNonAvailableStatus_ShouldReturnFalse(string status)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = status;

        // Act
        var result = seat.IsAvailable();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsReserved_WithReservedStatus_ShouldReturnTrue()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusReserved;

        // Act
        var result = seat.IsReserved();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSold_WithSoldStatus_ShouldReturnTrue()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusSold;

        // Act
        var result = seat.IsSold();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanBeReserved_WithAvailableStatus_ShouldReturnTrue()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusAvailable;

        // Act
        var result = seat.CanBeReserved();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(Seat.StatusReserved)]
    [InlineData(Seat.StatusSold)]
    public void CanBeReserved_WithNonAvailableStatus_ShouldReturnFalse(string status)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = status;

        // Act
        var result = seat.CanBeReserved();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(Seat.StatusAvailable)]
    [InlineData(Seat.StatusReserved)]
    public void CanBeSold_WithAvailableOrReservedStatus_ShouldReturnTrue(string status)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = status;

        // Act
        var result = seat.CanBeSold();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanBeSold_WithSoldStatus_ShouldReturnFalse()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusSold;

        // Act
        var result = seat.CanBeSold();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanBeReleased_WithReservedStatus_ShouldReturnTrue()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusReserved;

        // Act
        var result = seat.CanBeReleased();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(Seat.StatusAvailable)]
    [InlineData(Seat.StatusSold)]
    public void CanBeReleased_WithNonReservedStatus_ShouldReturnFalse(string status)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = status;

        // Act
        var result = seat.CanBeReleased();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Reserve_FromAvailableStatus_ShouldChangeStatusToReserved()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusAvailable;

        // Act
        seat.Reserve();

        // Assert
        seat.Status.Should().Be(Seat.StatusReserved);
    }

    [Theory]
    [InlineData(Seat.StatusReserved)]
    [InlineData(Seat.StatusSold)]
    public void Reserve_FromNonAvailableStatus_ShouldThrowInvalidOperationException(string status)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = status;

        // Act & Assert
        var action = () => seat.Reserve();
        action.Should().Throw<InvalidOperationException>()
              .WithMessage($"Cannot reserve seat. Current status: {status}");
    }

    [Theory]
    [InlineData(Seat.StatusAvailable)]
    [InlineData(Seat.StatusReserved)]
    public void Sell_FromAvailableOrReservedStatus_ShouldChangeStatusToSold(string initialStatus)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = initialStatus;

        // Act
        seat.Sell();

        // Assert
        seat.Status.Should().Be(Seat.StatusSold);
    }

    [Fact]
    public void Sell_FromSoldStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusSold;

        // Act & Assert
        var action = () => seat.Sell();
        action.Should().Throw<InvalidOperationException>()
              .WithMessage($"Cannot sell seat. Current status: {Seat.StatusSold}");
    }

    [Fact]
    public void Release_FromReservedStatus_ShouldChangeStatusToAvailable()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusReserved;

        // Act
        seat.Release();

        // Assert
        seat.Status.Should().Be(Seat.StatusAvailable);
    }

    [Theory]
    [InlineData(Seat.StatusAvailable)]
    [InlineData(Seat.StatusSold)]
    public void Release_FromNonReservedStatus_ShouldThrowInvalidOperationException(string status)
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = status;

        // Act & Assert
        var action = () => seat.Release();
        action.Should().Throw<InvalidOperationException>()
              .WithMessage($"Cannot release seat. Current status: {status}");
    }

    [Fact]
    public void ValidateBusinessRules_WithValidSeat_ShouldNotThrow()
    {
        // Arrange
        var seat = CreateValidSeat();

        // Act & Assert
        var action = () => seat.ValidateBusinessRules();
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateBusinessRules_WithEmptySectionCode_ShouldThrowArgumentException()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.SectionCode = "";

        // Act & Assert
        var action = () => seat.ValidateBusinessRules();
        action.Should().Throw<ArgumentException>()
              .WithMessage("Section code cannot be empty*")
              .And.ParamName.Should().Be("SectionCode");
    }

    [Fact]
    public void ValidateBusinessRules_WithZeroRowNumber_ShouldThrowArgumentException()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.RowNumber = 0;

        // Act & Assert
        var action = () => seat.ValidateBusinessRules();
        action.Should().Throw<ArgumentException>()
              .WithMessage("Row number must be greater than zero*")
              .And.ParamName.Should().Be("RowNumber");
    }

    [Fact]
    public void ValidateBusinessRules_WithZeroSeatNumber_ShouldThrowArgumentException()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.SeatNumber = 0;

        // Act & Assert
        var action = () => seat.ValidateBusinessRules();
        action.Should().Throw<ArgumentException>()
              .WithMessage("Seat number must be greater than zero*")
              .And.ParamName.Should().Be("SeatNumber");
    }

    [Fact]
    public void ValidateBusinessRules_WithZeroPrice_ShouldThrowArgumentException()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Price = 0;

        // Act & Assert
        var action = () => seat.ValidateBusinessRules();
        action.Should().Throw<ArgumentException>()
              .WithMessage("Price must be greater than zero*")
              .And.ParamName.Should().Be("Price");
    }

    [Fact]
    public void ValidateBusinessRules_WithInvalidStatus_ShouldThrowArgumentException()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = "invalid";

        // Act & Assert
        var action = () => seat.ValidateBusinessRules();
        action.Should().Throw<ArgumentException>()
              .WithMessage("Invalid status: invalid*")
              .And.ParamName.Should().Be("Status");
    }

    [Fact]
    public void SeatStatusConstants_ShouldHaveCorrectValues()
    {
        // Act & Assert
        Seat.StatusAvailable.Should().Be("available");
        Seat.StatusReserved.Should().Be("reserved");
        Seat.StatusSold.Should().Be("sold");
        Seat.StatusUnavailable.Should().Be("unavailable");
    }

    #region Seat Unavailable Status Tests - T106

    [Fact]
    public void Seat_IsUnavailable_WithUnavailableStatus_ShouldReturnTrue()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusUnavailable;

        // Act & Assert
        seat.IsUnavailable().Should().BeTrue();
    }

    [Fact]
    public void Seat_IsUnavailable_WithAvailableStatus_ShouldReturnFalse()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusAvailable;

        // Act & Assert
        seat.IsUnavailable().Should().BeFalse();
    }

    [Fact]
    public void Seat_MakeUnavailable_FromAvailable_ShouldChangeToUnavailable()
    {
        // Arrange - Following Gherkin: seats become unavailable when event is deactivated
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusAvailable;

        // Act
        seat.MakeUnavailable();

        // Assert
        seat.Status.Should().Be(Seat.StatusUnavailable);
    }

    [Fact]
    public void Seat_MakeUnavailable_FromReserved_ShouldThrowException()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusReserved;

        // Act & Assert
        var action = () => seat.MakeUnavailable();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot make seat unavailable. Seat has active reservation or is sold. Current status: reserved");
    }

    [Fact]
    public void Seat_MakeUnavailable_FromSold_ShouldThrowException()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusSold;

        // Act & Assert
        var action = () => seat.MakeUnavailable();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot make seat unavailable. Seat has active reservation or is sold. Current status: sold");
    }

    [Fact]
    public void Seat_MakeAvailable_FromUnavailable_ShouldChangeToAvailable()
    {
        // Arrange - Following Gherkin: seats become available when event is reactivated
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusUnavailable;

        // Act
        seat.MakeAvailable();

        // Assert
        seat.Status.Should().Be(Seat.StatusAvailable);
    }

    [Fact]
    public void Seat_MakeAvailable_FromAvailable_ShouldThrowException()
    {
        // Arrange
        var seat = CreateValidSeat();
        seat.Status = Seat.StatusAvailable;

        // Act & Assert
        var action = () => seat.MakeAvailable();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot make seat available. Current status: available. Only unavailable seats can be made available.");
    }

    #endregion

    // Test Helper Methods
    private static Seat CreateValidSeat()
    {
        return new Seat
        {
            Id = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            SectionCode = "VIP",
            RowNumber = 5,
            SeatNumber = 12,
            Price = 125.50m,
            Status = Seat.StatusAvailable
        };
    }
}