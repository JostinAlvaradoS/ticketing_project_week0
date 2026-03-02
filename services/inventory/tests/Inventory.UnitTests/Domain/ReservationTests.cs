using FluentAssertions;
using Inventory.Domain.Entities;
using Xunit;

namespace Inventory.UnitTests.Domain;

public class ReservationTests
{
    [Fact]
    public void Reservation_Should_Initialize_Correctly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var customerId = "customer-123";
        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.AddMinutes(15);
        var status = "active";

        // Act
        var reservation = new Reservation
        {
            Id = id,
            SeatId = seatId,
            CustomerId = customerId,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            Status = status
        };

        // Assert
        reservation.Id.Should().Be(id);
        reservation.SeatId.Should().Be(seatId);
        reservation.CustomerId.Should().Be(customerId);
        reservation.CreatedAt.Should().Be(createdAt);
        reservation.ExpiresAt.Should().Be(expiresAt);
        reservation.Status.Should().Be(status);
    }
}
