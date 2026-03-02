using Fulfillment.Application.Ports;
using FluentAssertions;

namespace Fulfillment.Application.UnitTests.Ports;

public class OrderDetailsDtoTests
{
    [Fact]
    public void OrderDetailsDto_ShouldStoreValuesCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var email = "test@example.com";
        var eventId = Guid.NewGuid();
        var eventName = "Test Event";
        var seatNumber = "A1";
        var price = 99.99m;
        var currency = "USD";

        // Act
        var dto = new OrderDetailsDto(orderId, email, eventId, eventName, seatNumber, price, currency);

        // Assert
        dto.OrderId.Should().Be(orderId);
        dto.CustomerEmail.Should().Be(email);
        dto.EventId.Should().Be(eventId);
        dto.EventName.Should().Be(eventName);
        dto.SeatNumber.Should().Be(seatNumber);
        dto.Price.Should().Be(price);
        dto.Currency.Should().Be(currency);
    }
}
