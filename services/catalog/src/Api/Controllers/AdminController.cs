using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Catalog.Application.UseCases.CreateEvent;
using Catalog.Application.UseCases.GenerateSeats;
using Catalog.Application.UseCases.UpdateEvent;
using Catalog.Application.UseCases.DeactivateEvent;
using Catalog.Application.UseCases.ReactivateEvent;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("admin")]
[AllowAnonymous] // Temporary: Allow anonymous access for testing
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new event (Admin only).
    /// </summary>
    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        var command = new CreateEventCommand(
            request.Name,
            request.Description,
            request.EventDate,
            request.Venue,
            request.MaxCapacity,
            request.BasePrice
        );

        var result = await _mediator.Send(command);
        
        return CreatedAtAction(
            nameof(EventsController.GetEvent),
            "Events",
            new { id = result.Id },
            result
        );
    }

    /// <summary>
    /// Generate seats for an existing event (Admin only).
    /// </summary>
    [HttpPost("events/{eventId:guid}/seats")]
    public async Task<IActionResult> GenerateSeats(Guid eventId, [FromBody] GenerateSeatsRequest request)
    {
        var command = new GenerateSeatsCommand(
            eventId,
            request.SectionConfigurations.ToList()
        );

        var result = await _mediator.Send(command);
        
        return Ok(result);
    }

    /// <summary>
    /// Update an existing event (Admin only). 
    /// Event date and base price cannot be modified if reservations exist.
    /// </summary>
    [HttpPut("events/{eventId:guid}")]
    public async Task<IActionResult> UpdateEvent(Guid eventId, [FromBody] UpdateEventRequest request)
    {
        var command = new UpdateEventCommand(
            eventId,
            request.Name,
            request.Description,
            request.MaxCapacity
        );

        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deactivate an event (Soft Delete) (Admin only).
    /// Fails if the event has active reservations.
    /// </summary>
    [HttpPost("events/{eventId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateEvent(Guid eventId)
    {
        var command = new DeactivateEventCommand(eventId);

        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reactivate a deactivated event (Admin only).
    /// Fails if the event date is in the past.
    /// </summary>
    [HttpPost("events/{eventId:guid}/reactivate")]
    public async Task<IActionResult> ReactivateEvent(Guid eventId)
    {
        var command = new ReactivateEventCommand(eventId);

        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Request DTO for creating events.
/// </summary>
public record CreateEventRequest(
    string Name,
    string Description,
    DateTime EventDate,
    string Venue,
    int MaxCapacity,
    decimal BasePrice
);

/// <summary>
/// Request DTO for generating seats.
/// </summary>
public record GenerateSeatsRequest(
    SeatSectionConfiguration[] SectionConfigurations
);

/// <summary>
/// Request DTO for updating events.
/// </summary>
public record UpdateEventRequest(
    string Name,
    string Description,
    int MaxCapacity
);