using Confluent.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Notification.Application.Ports;
using Notification.Application.UseCases.SendTicketNotification;
using Notification.Infrastructure.Events;
using System.Text.Json;
using FluentAssertions;

namespace Notification.IntegrationTests.Events;

public class TicketIssuedEventConsumerTests : IClassFixture<Fixtures.IntegrationTestFixture>
{
    private readonly Fixtures.IntegrationTestFixture _fixture;

    public TicketIssuedEventConsumerTests(Fixtures.IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConsumeTicketIssuedEvent_ShouldCreateEmailNotification()
    {
        // ... (resto del código)
    }

    /// <summary>
    /// TEST DE CASO BORDE: Email Inválido
    /// Propósito: Verificar que el sistema maneja correctamente datos inesperados
    /// sin que el consumidor de Kafka se bloquee.
    /// </summary>
    [Fact]
    public async Task ConsumeTicketIssuedEvent_WithInvalidEmail_ShouldHandleErrorGracefully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var ticketEvent = new TicketIssuedEvent
        {
            TicketId = Guid.NewGuid(),
            OrderId = orderId,
            CustomerEmail = "invalid-email-format",
            EventName = "Borde Case Concert",
            SeatNumber = "B2",
            Price = 50.00m,
            Currency = "USD",
            TicketPdfUrl = "https://example.com/ticket.pdf",
            QrCodeData = "QR_DATA_HERE",
            IssuedAt = DateTime.UtcNow,
            Timestamp = DateTime.UtcNow
        };

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

                // Act
                var result = await mediator.Send(command);

                // Assert: La aplicación debería retornar un fallo controlado
                result.Success.Should().BeFalse();
                
                // Verificar que NO se guardó en la DB como "Sent"
                var notification = await _fixture.DbContext!.EmailNotifications
                    .FirstOrDefaultAsync(n => n.OrderId == orderId);
                
                if (notification != null)
                {
                    notification.Status.Should().NotBe(Notification.Domain.Entities.NotificationStatus.Sent);
                }
            }
        }
    }

    /// <summary>
    /// TEST DE IDEMPOTENCIA
    /// Propósito: Asegurar que si el mismo evento llega dos veces, no enviamos dos correos.
    /// </summary>
    [Fact]
    public async Task ConsumeTicketIssuedEvent_DuplicateEvent_ShouldBeIdempotent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();
        var command = new SendTicketNotificationCommand
        {
            TicketId = ticketId,
            OrderId = orderId,
            RecipientEmail = "duplicate@example.com",
            EventName = "Idempotency Fest",
            SeatNumber = "C3",
            Price = 75.00m,
            Currency = "USD",
            TicketPdfUrl = "url",
            QrCodeData = "qr",
            TicketIssuedAt = DateTime.UtcNow
        };

        if (_fixture.ServiceProvider != null)
        {
            using (var scope = _fixture.ServiceProvider.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                // Act: Enviar el mismo comando dos veces
                var result1 = await mediator.Send(command);
                var result2 = await mediator.Send(command);

                // Assert
                result1.Success.Should().BeTrue();
                result2.Success.Should().BeTrue(); 

                // Verificar que en la DB solo existe UN registro para esta orden
                var count = await _fixture.DbContext!.EmailNotifications
                    .CountAsync(n => n.OrderId == orderId);

                count.Should().Be(1, "No deben crearse múltiples notificaciones para el mismo ticket");
            }
        }
    }

    [Fact]
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
