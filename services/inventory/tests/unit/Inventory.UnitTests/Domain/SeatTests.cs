using FluentAssertions;
using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.UnitTests.Domain;

public class SeatTests
{
    [Fact]
    public void Seat_Should_Initialize_Correctly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var section = "A";
        var row = "1";
        var number = 10;

        // Act
        var seat = new Seat
        {
            Id = id,
            Section = section,
            Row = row,
            Number = number
            // Reserved defaults to false
        };

        // Assert
        seat.Id.Should().Be(id);
        seat.Section.Should().Be(section);
        seat.Row.Should().Be(row);
        seat.Number.Should().Be(number);
        seat.Reserved.Should().BeFalse();
    }
}
