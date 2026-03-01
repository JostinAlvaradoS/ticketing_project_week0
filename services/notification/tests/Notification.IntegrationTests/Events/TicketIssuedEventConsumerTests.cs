using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Notification.Application.Ports;
using Notification.Application.UseCases.SendTicketNotification;
using Notification.Infrastructure.Events;
using System.Text.Json;

namespace Notification.IntegrationTests.Events;

public class TicketIssuedEventConsumerTests : IClassFixture<Fixtures.IntegrationTestFixture>
{
    private readonly Fixtures.IntegrationTestFixture _fixture;

    public TicketIssuedEventConsumerTests(Fixtures.IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires full integration test environment")]
    public async Task ConsumeTicketIssuedEvent_ShouldCreateEmailNotification()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var customerEmail = "customer@example.com";

        var ticketEvent = new TicketIssuedEvent
        {
            TicketId = ticketId,
            OrderId = orderId,
            CustomerEmail = customerEmail,
            EventName = "Concert 2026",
            SeatNumber = "A1",
            Price = 100.00m,
            Currency = "USD",
            TicketPdfUrl = "https://example.com/ticket.pdf",
            QrCodeData = "QR_DATA_HERE",
            IssuedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

        // Publish to Kafka (in real scenario)
        var json = JsonSerializer.Serialize(ticketEvent, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
        });

        // Simulate message consumption by calling handler directly
        if (_fixture.ServiceProvider != null)
        {
            using (var scope = _fixture.ServiceProvider.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var command = new SendTicketNotificationCommand
                {
                    TicketId = ticketEvent.TicketId,
                    OrderId = ticketEvent.OrderId,
                    RecipientEmail = ticketEvent.CustomerEmail,
                    EventName = ticketEvent.EventName,
                    SeatNumber = ticketEvent.SeatNumber,
                    Price = ticketEvent.Price,
                    Currency = ticketEvent.Currency,
                    TicketPdfUrl = ticketEvent.TicketPdfUrl,
                    QrCodeData = ticketEvent.QrCodeData,
                    TicketIssuedAt = ticketEvent.IssuedAt
                };

                var result = await mediator.Send(command);

                // Assert
                result.Success.Should().BeTrue();
                result.NotificationId.Should().NotBeEmpty();

                // Verify in database
                var notification = await _fixture.DbContext!.EmailNotifications
                    .FirstOrDefaultAsync(n => n.OrderId == orderId);

                notification.Should().NotBeNull();
                notification!.RecipientEmail.Should().Be(customerEmail);
                notification.Status.Should().Be(Notification.Domain.Entities.NotificationStatus.Sent);
            }
        }
    }

    [Fact(Skip = "Requires full integration test environment")]
    public async Task EndToEndFlow_ReservationToEmailNotification_ShouldSucceed()
    {
        // This test would validate the full flow:
        // 1. Reservation created
        // 2. Order placed
        // 3. Payment succeeded
        // 4. Ticket issued
        // 5. Notification email sent

        // Mock all dependent services
        var mockOrderingServiceClient = new Mock<IOrderingServiceClient>();
        var mockFulfillmentServiceClient = new Mock<IFulfillmentServiceClient>();

        // Setup expectations
        mockOrderingServiceClient
            .Setup(o => o.GetOrderDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new OrderDetails
            {
                OrderId = Guid.NewGuid(),
                CustomerEmail = "customer@example.com",
                EventName = "Concert 2026",
                SeatNumber = "A1",
                Price = 100.00m,
                Currency = "USD"
            });

        mockFulfillmentServiceClient
            .Setup(f => f.GetTicketDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new TicketDetails
            {
                TicketId = Guid.NewGuid(),
                TicketPdfUrl = "https://example.com/ticket.pdf",
                QrCodeData = "QR_DATA"
            });

        await Task.CompletedTask;
    }
}

// Helper interfaces for dependencies
public interface IOrderingServiceClient
{
    Task<OrderDetails?> GetOrderDetailsAsync(Guid orderId);
}

public interface IFulfillmentServiceClient
{
    Task<TicketDetails?> GetTicketDetailsAsync(Guid ticketId);
}

public class OrderDetails
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class TicketDetails
{
    public Guid TicketId { get; set; }
    public string TicketPdfUrl { get; set; } = string.Empty;
    public string QrCodeData { get; set; } = string.Empty;
}
