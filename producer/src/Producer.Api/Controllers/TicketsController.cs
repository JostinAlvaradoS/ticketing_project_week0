
using Microsoft.AspNetCore.Mvc;
using Producer.Application.Dtos;
using Producer.Application.Ports.Inbound;

namespace Producer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly IReserveTicketUseCase _reserveTicketUseCase;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        IReserveTicketUseCase reserveTicketUseCase,
        ILogger<TicketsController> logger)
    {
        _reserveTicketUseCase = reserveTicketUseCase;
        _logger = logger;
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveTicket(
        [FromBody] ReserveTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest("La solicitud no puede estar vacía");

        try
        {
            var result = await _reserveTicketUseCase.ExecuteAsync(request, cancellationToken);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            _logger.LogInformation(
                "Ticket reservado exitosamente. TicketId: {TicketId}",
                request.TicketId);

            return Accepted(new { message = result.Message, ticketId = result.TicketId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reservar ticket. TicketId: {TicketId}", request.TicketId);
            return StatusCode(500, new { message = "Error al procesar la reserva" });
        }
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
