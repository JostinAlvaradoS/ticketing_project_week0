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
        var seatId = Guid.NewGuid();
        var customerId = "customer-123";

        // Act
        var reservation = Reservation.Create(seatId, customerId);

        // Assert
        reservation.Id.Should().NotBeEmpty();
        reservation.SeatId.Should().Be(seatId);
        reservation.CustomerId.Should().Be(customerId);
        reservation.Status.Should().Be(Reservation.StatusActive);
        reservation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        reservation.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(1), TimeSpan.FromSeconds(2)); // ttlMinutes default = 1 en demo
    }

    [Theory]
    [InlineData(14, false)] // TC-P1-01: 14:01 desde creación (no expirado)
    [InlineData(15, true)]  // Límite: 15:01 desde creación (expirado)
    [InlineData(16, true)]  // TC-P1-01: 16:01 desde creación (expirado)
    public void IsExpired_Should_Return_Correct_Status_Based_On_TTL(int minutesPassed, bool expectedExpired)
    {
        // Arrange — TTL de 15 minutos
        var reservation = Reservation.Create(Guid.NewGuid(), "customer-123", ttlMinutes: 15);
        var checkTime = reservation.CreatedAt.AddMinutes(minutesPassed).AddSeconds(1);

        // Act
        var result = reservation.IsExpired(checkTime);

        // Assert
        result.Should().Be(expectedExpired);
    }

    // ── Branch coverage: guards de Create() ─────────────────────────

    [Fact]
    public void Create_WithEmptySeatId_ShouldThrowArgumentException()
    {
        // cubre la rama: seatId == Guid.Empty → true
        var act = () => Reservation.Create(Guid.Empty, "customer-123");
        act.Should().Throw<ArgumentException>().WithParameterName("seatId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCustomerId_ShouldThrowArgumentException(string? customerId)
    {
        // cubre la rama: IsNullOrWhiteSpace(customerId) → true (null, vacío y whitespace)
        var act = () => Reservation.Create(Guid.NewGuid(), customerId!);
        act.Should().Throw<ArgumentException>().WithParameterName("customerId");
    }

    [Fact]
    public void Create_WithZeroTtl_ShouldThrowArgumentException()
    {
        // cubre la rama: ttlMinutes <= 0 → true
        var act = () => Reservation.Create(Guid.NewGuid(), "customer-123", ttlMinutes: 0);
        act.Should().Throw<ArgumentException>().WithParameterName("ttlMinutes");
    }

    [Fact]
    public void IsExpired_WhenStatusIsExpired_ShouldReturnTrueRegardlessOfTime()
    {
        // cubre la rama: Status == StatusExpired → short-circuit true (sin evaluar ExpiresAt)
        var reservation = Reservation.Create(Guid.NewGuid(), "customer-123", ttlMinutes: 60);
        reservation.Status = Reservation.StatusExpired;

        var result = reservation.IsExpired(reservation.CreatedAt); // tiempo anterior a ExpiresAt

        result.Should().BeTrue();
    }
}
