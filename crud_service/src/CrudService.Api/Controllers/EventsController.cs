using CrudService.Application.Dtos;
using CrudService.Application.Ports.Inbound;
using Microsoft.AspNetCore.Mvc;

namespace CrudService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventCommands _eventCommands;
    private readonly IEventQueries _eventQueries;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventCommands eventCommands,
        IEventQueries eventQueries,
        ILogger<EventsController> logger)
    {
        _eventCommands = eventCommands;
        _eventQueries = eventQueries;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
    {
        try
        {
            var events = await _eventQueries.GetAllEventsAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener eventos");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error al obtener eventos");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetEvent(long id)
    {
        try
        {
            var @event = await _eventQueries.GetEventByIdAsync(id);
            if (@event == null)
                return NotFound($"Evento {id} no encontrado");

            return Ok(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener evento {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("El nombre del evento es requerido");

            if (request.StartsAt == default)
                return BadRequest("La fecha de inicio es requerida");

            var @event = await _eventCommands.CreateEventAsync(request);
            _logger.LogInformation("Evento creado: {EventId}", @event.Id);
            return CreatedAtAction(nameof(GetEvent), new { id = @event.Id }, @event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear evento");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EventDto>> UpdateEvent(long id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            var updated = await _eventCommands.UpdateEventAsync(id, request);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Evento {id} no encontrado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar evento {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(long id)
    {
        try
        {
            var deleted = await _eventCommands.DeleteEventAsync(id);
            if (!deleted)
                return NotFound($"Evento {id} no encontrado");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar evento {EventId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
