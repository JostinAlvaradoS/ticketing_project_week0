using Microsoft.AspNetCore.Mvc;
using Producer.Models;
using Producer.Services;

namespace Producer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentRequestPublisher _publisher;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentRequestPublisher publisher,
        ILogger<PaymentsController> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Recibe la solicitud de pago y la publica a RabbitMQ.
    /// La decision de aprobar o rechazar la toma el PaymentService (consumer).
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "La solicitud no puede estar vacía" });

        if (request.TicketId <= 0)
            return BadRequest(new { message = "TicketId debe ser mayor a 0" });

        if (request.EventId <= 0)
            return BadRequest(new { message = "EventId debe ser mayor a 0" });

        if (request.AmountCents <= 0)
            return BadRequest(new { message = "AmountCents debe ser mayor a 0" });

        if (string.IsNullOrWhiteSpace(request.PaymentBy))
            return BadRequest(new { message = "PaymentBy es requerido" });

        if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
            return BadRequest(new { message = "PaymentMethodId es requerido" });

        try
        {
            var paymentEvent = new PaymentRequestedEvent
            {
                TicketId = request.TicketId,
                EventId = request.EventId,
                AmountCents = request.AmountCents,
                Currency = request.Currency,
                PaymentBy = request.PaymentBy,
                PaymentMethodId = request.PaymentMethodId,
                TransactionRef = request.TransactionRef ?? $"TXN-{Guid.NewGuid()}",
                RequestedAt = DateTime.UtcNow
            };

            await _publisher.PublishPaymentRequestedAsync(paymentEvent, cancellationToken);

            _logger.LogInformation(
                "Payment request published. TicketId={TicketId}, EventId={EventId}",
                request.TicketId,
                request.EventId);

            return Accepted(new
            {
                message = "Solicitud de pago encolada para procesamiento",
                ticketId = request.TicketId,
                eventId = request.EventId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error publishing payment request. TicketId={TicketId}",
                request.TicketId);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Error al encolar la solicitud de pago"
            });
        }
    }
}
