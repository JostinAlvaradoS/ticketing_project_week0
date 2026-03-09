using Fulfillment.Domain.Entities;

namespace Fulfillment.Domain.UnitTests.Entities;

public class TicketEntityTests
{
    [Fact]
    public void When_Creating_Ticket_With_Valid_Data_Should_Create_Successfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Act
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            CustomerEmail = "test@example.com",
            EventName = "Concierto Foo Fighters",
            SeatNumber = "A-15",
            Price = 150.00m,
            Currency = "USD",
            Status = TicketStatus.Pending,
            QrCodeData = $"{orderId}:A-15:{eventId}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        ticket.Id.Should().NotBeEmpty();
        ticket.OrderId.Should().Be(orderId);
        ticket.CustomerEmail.Should().Be("test@example.com");
        ticket.EventName.Should().Be("Concierto Foo Fighters");
        ticket.SeatNumber.Should().Be("A-15");
        ticket.Price.Should().Be(150.00m);
        ticket.Currency.Should().Be("USD");
        ticket.Status.Should().Be(TicketStatus.Pending);
        ticket.QrCodeData.Should().NotBeEmpty();
    }

    [Fact]
    public void When_Creating_Ticket_Without_OrderId_Should_Allow_Creation()
    {
        // Arrange & Act
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.Empty, // Should work, no validation at entity level
            CustomerEmail = "test@example.com",
            EventName = "Test Event",
            SeatNumber = "A-1",
            Price = 100m,
            Status = TicketStatus.Pending
        };

        // Assert
        ticket.OrderId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void When_Ticket_Status_Changes_Should_Update_Timestamp()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Status = TicketStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var originalUpdatedAt = ticket.UpdatedAt;
        System.Threading.Thread.Sleep(10); // Small delay
        ticket.Status = TicketStatus.Generated;
        ticket.UpdatedAt = DateTime.UtcNow;

        // Assert
        ticket.Status.Should().Be(TicketStatus.Generated);
        ticket.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData(TicketStatus.Pending)]
    [InlineData(TicketStatus.Generated)]
    [InlineData(TicketStatus.Failed)]
    [InlineData(TicketStatus.Delivered)]
    public void All_TicketStatus_Values_Should_Be_Valid(TicketStatus status)
    {
        // Arrange & Act
        var ticket = new Ticket { Status = status };

        // Assert
        ticket.Status.Should().Be(status);
    }

    [Fact]
    public void Ticket_QrCodeData_Format_Should_Be_Valid()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var qrData = $"{orderId}:A-15:{eventId}";

        // Act
        var ticket = new Ticket { QrCodeData = qrData };
        var parts = ticket.QrCodeData.Split(':');

        // Assert
        parts.Should().HaveCount(3);
        parts[0].Should().Be(orderId.ToString());
        parts[1].Should().Be("A-15");
        parts[2].Should().Be(eventId.ToString());
    }
}
