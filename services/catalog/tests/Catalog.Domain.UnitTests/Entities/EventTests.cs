using Catalog.Domain.Entities;
using FluentAssertions;

namespace Catalog.Domain.UnitTests.Entities;

public class EventTests
{
    [Fact]
    public void IsValidForCreation_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var eventEntity = CreateValidEvent();

        // Act
        var result = eventEntity.IsValidForCreation();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidForCreation_WithInvalidName_ShouldReturnFalse(string invalidName)
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        eventEntity.Name = invalidName;

        // Act
        var result = eventEntity.IsValidForCreation();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidForCreation_WithPastEventDate_ShouldReturnFalse()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        eventEntity.EventDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var result = eventEntity.IsValidForCreation();

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void IsValidForCreation_WithInvalidBasePrice_ShouldReturnFalse(decimal invalidPrice)
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        eventEntity.BasePrice = invalidPrice;

        // Act
        var result = eventEntity.IsValidForCreation();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBookable_WithFutureEventAndAvailableSeats_ShouldReturnTrue()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 2);

        // Act
        var result = eventEntity.IsBookable();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBookable_WithPastEvent_ShouldReturnFalse()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        eventEntity.EventDate = DateTime.UtcNow.AddDays(-1);
        AddAvailableSeats(eventEntity, 2);

        // Act
        var result = eventEntity.IsBookable();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBookable_WithNoAvailableSeats_ShouldReturnFalse()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddSoldSeats(eventEntity, 2);

        // Act
        var result = eventEntity.IsBookable();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAvailableSeats_WithAvailableSeats_ShouldReturnTrue()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 1);

        // Act
        var result = eventEntity.HasAvailableSeats();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasAvailableSeats_WithNoAvailableSeats_ShouldReturnFalse()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddSoldSeats(eventEntity, 2);

        // Act
        var result = eventEntity.HasAvailableSeats();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAvailableSeatsCount_WithMixedSeatStatuses_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 3);
        AddReservedSeats(eventEntity, 2);
        AddSoldSeats(eventEntity, 1);

        // Act
        var result = eventEntity.GetAvailableSeatsCount();

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void GetReservedSeatsCount_WithMixedSeatStatuses_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 3);
        AddReservedSeats(eventEntity, 2);
        AddSoldSeats(eventEntity, 1);

        // Act
        var result = eventEntity.GetReservedSeatsCount();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void GetSoldSeatsCount_WithMixedSeatStatuses_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 3);
        AddReservedSeats(eventEntity, 2);
        AddSoldSeats(eventEntity, 1);

        // Act
        var result = eventEntity.GetSoldSeatsCount();

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetAvailableSeats_ShouldReturnOnlyAvailableSeats()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddAvailableSeats(eventEntity, 2);
        AddReservedSeats(eventEntity, 1);
        AddSoldSeats(eventEntity, 1);

        // Act
        var result = eventEntity.GetAvailableSeats();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.IsAvailable());
    }

    [Fact]
    public void GetSeatsBySection_WithSpecificSection_ShouldReturnOnlySeatsFromThatSection()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        AddSeatToSection(eventEntity, "VIP", Seat.StatusAvailable);
        AddSeatToSection(eventEntity, "VIP", Seat.StatusReserved);
        AddSeatToSection(eventEntity, "General", Seat.StatusAvailable);

        // Act
        var result = eventEntity.GetSeatsBySection("VIP");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.SectionCode == "VIP");
    }

    [Fact]
    public void ValidateBusinessRules_WithValidEvent_ShouldNotThrow()
    {
        // Arrange
        var eventEntity = CreateValidEvent();

        // Act & Assert
        var action = () => eventEntity.ValidateBusinessRules();
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateBusinessRules_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        eventEntity.Name = "";

        // Act & Assert
        var action = () => eventEntity.ValidateBusinessRules();
        action.Should().Throw<ArgumentException>()
              .WithMessage("Event name cannot be empty*")
              .And.ParamName.Should().Be("Name");
    }

    [Fact]
    public void ValidateBusinessRules_WithPastEventDate_ShouldThrowArgumentException()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        eventEntity.EventDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        var action = () => eventEntity.ValidateBusinessRules();
        action.Should().Throw<ArgumentException>()
              .WithMessage("Event date must be in the future*")
              .And.ParamName.Should().Be("EventDate");
    }

    [Fact]
    public void ValidateBusinessRules_WithNegativeBasePrice_ShouldThrowArgumentException()
    {
        // Arrange
        var eventEntity = CreateValidEvent();
        eventEntity.BasePrice = -10;

        // Act & Assert
        var action = () => eventEntity.ValidateBusinessRules();
        action.Should().Throw<ArgumentException>()
              .WithMessage("Base price must be greater than zero*")
              .And.ParamName.Should().Be("BasePrice");
    }

    // Test Helper Methods
    private static Event CreateValidEvent()
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Name = "Test Concert",
            Description = "A great test concert",
            EventDate = DateTime.UtcNow.AddDays(30),
            BasePrice = 50.00m
        };
    }

    private static void AddAvailableSeats(Event eventEntity, int count)
    {
        for (int i = 0; i < count; i++)
        {
            eventEntity.Seats.Add(CreateSeat(eventEntity.Id, Seat.StatusAvailable, i + 1));
        }
    }

    private static void AddReservedSeats(Event eventEntity, int count)
    {
        for (int i = 0; i < count; i++)
        {
            eventEntity.Seats.Add(CreateSeat(eventEntity.Id, Seat.StatusReserved, i + 100));
        }
    }

    private static void AddSoldSeats(Event eventEntity, int count)
    {
        for (int i = 0; i < count; i++)
        {
            eventEntity.Seats.Add(CreateSeat(eventEntity.Id, Seat.StatusSold, i + 200));
        }
    }

    private static void AddSeatToSection(Event eventEntity, string section, string status)
    {
        var seat = CreateSeat(eventEntity.Id, status, 1);
        seat.SectionCode = section;
        eventEntity.Seats.Add(seat);
    }

    private static Seat CreateSeat(Guid eventId, string status, int seatNumber)
    {
        return new Seat
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            SectionCode = "General",
            RowNumber = 1,
            SeatNumber = seatNumber,
            Price = 75.00m,
            Status = status
        };
    }
}