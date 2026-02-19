using CrudService.Application.Dtos;
using CrudService.Application.Ports.Inbound;
using Microsoft.AspNetCore.Mvc;

namespace CrudService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketCommands _ticketCommands;
    private readonly ITicketQueries _ticketQueries;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(
        ITicketCommands ticketCommands,
        ITicketQueries ticketQueries,
        ILogger<TicketsController> logger)
    {
        _ticketCommands = ticketCommands;
        _ticketQueries = ticketQueries;
        _logger = logger;
    }

    [HttpGet("event/{eventId}")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetTicketsByEvent(long eventId)
    {
        try
        {
            var tickets = await _ticketQueries.GetTicketsByEventAsync(eventId);
            return Ok(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tickets del evento {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(long id)
    {
        try
        {
            var ticket = await _ticketQueries.GetTicketByIdAsync(id);
            if (ticket == null)
                return NotFound($"Ticket {id} no encontrado");

            return Ok(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ticket {TicketId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> CreateTickets([FromBody] CreateTicketsRequest request)
    {
        try
        {
            if (request.EventId <= 0)
                return BadRequest("EventId debe ser mayor a 0");

            if (request.Quantity <= 0 || request.Quantity > 1000)
                return BadRequest("Quantity debe estar entre 1 y 1000");

            var tickets = await _ticketCommands.CreateTicketsAsync(request.EventId, request.Quantity);
            _logger.LogInformation("Creados {Quantity} tickets para evento {EventId}", request.Quantity, request.EventId);
            return CreatedAtAction(nameof(GetTicketsByEvent), new { eventId = request.EventId }, tickets);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear tickets");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<TicketDto>> UpdateTicketStatus(long id, [FromBody] UpdateTicketStatusRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.NewStatus))
                return BadRequest("NewStatus es requerido");

            var ticket = await _ticketCommands.UpdateTicketStatusAsync(id, request.NewStatus, request.Reason);
            return Ok(ticket);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Ticket {id} no encontrado");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar status del ticket {TicketId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("{id}/release")]
    public async Task<ActionResult<TicketDto>> ReleaseTicket(long id, [FromQuery] string? reason = null)
    {
        try
        {
            var ticket = await _ticketCommands.ReleaseTicketAsync(id, reason);
            _logger.LogInformation("Ticket {TicketId} liberado. Razón: {Reason}", id, reason ?? "No especificada");
            return Ok(ticket);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Ticket {id} no encontrado");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al liberar ticket {TicketId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("expired/list")]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetExpiredTickets()
    {
        try
        {
            var expiredTickets = await _ticketQueries.GetExpiredTicketsAsync();
            return Ok(expiredTickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener tickets expirados");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
