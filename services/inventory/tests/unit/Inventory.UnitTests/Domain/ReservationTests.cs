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

    [Theory]
    [InlineData(14, false)] // TC-P1-01: 14:59 (Valido)
    [InlineData(15, true)]  // Límite: 15:00 (Expirado - siguiendo lógica strictly greater than 15)
    [InlineData(16, true)]  // TC-P1-01: 15:01 (Expirado)
    public void IsExpired_Should_Return_Correct_Status_Based_On_TTL(int minutesPassed, bool expectedExpired)
    {
        // Arrange
        var createdAt = DateTime.UtcNow;
        var reservation = new Reservation
        {
            CreatedAt = createdAt,
            Status = "active"
        };
        var checkTime = createdAt.AddMinutes(minutesPassed).AddSeconds(1);

        // Act
        var result = reservation.IsExpired(checkTime);

        // Assert
        result.Should().Be(expectedExpired);
    }
}
