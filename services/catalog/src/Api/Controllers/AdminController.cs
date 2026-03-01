using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Catalog.Application.UseCases.CreateEvent;
using Catalog.Application.UseCases.GenerateSeats;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Policy = "RequireAdmin")]
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