using Microsoft.AspNetCore.Mvc;
using Producer.Application.Dtos;
using Producer.Application.Ports.Inbound;

namespace Producer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IProcessPaymentUseCase _processPaymentUseCase;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IProcessPaymentUseCase processPaymentUseCase,
        ILogger<PaymentsController> logger)
    {
        _processPaymentUseCase = processPaymentUseCase;
        _logger = logger;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "La solicitud no puede estar vacía" });

        try
        {
            var result = await _processPaymentUseCase.ExecuteAsync(request, cancellationToken);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Accepted(new
            {
                message = "Pago encolado para procesamiento",
                ticketId = result.TicketId,
                eventId = result.EventId,
                status = result.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar pago: TicketId={TicketId}", request.TicketId);
            return StatusCode(500, new { message = "Error al procesar el pago", error = ex.Message });
        }
    }
}
