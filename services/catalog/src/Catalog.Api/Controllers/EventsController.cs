using Microsoft.AspNetCore.Mvc;
using MediatR;
using Catalog.Application.UseCases.GetEventSeatmap;
using Catalog.Application.UseCases.GetAllEvents;
using Catalog.Application.UseCases.GetEvent;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all available events (without seatmap details).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllEvents()
    {
        var query = new GetAllEventsQuery();
        var result = await _mediator.Send(query);
        
        return Ok(result);
    }

    /// <summary>
    /// Get a specific event by ID (without seatmap details).
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(Guid id)
    {
        var query = new GetEventQuery(id);
        var result = await _mediator.Send(query);
        
        return result is not null ? Ok(result) : NotFound();
    }

    /// <summary>
    /// Get a specific event with its complete seatmap.
    /// </summary>
    [HttpGet("{id}/seatmap")]
    public async Task<IActionResult> GetEventSeatmap(Guid id)
    {
        var query = new GetEventSeatmapQuery(id);
        var result = await _mediator.Send(query);
        
        return result is not null ? Ok(result) : NotFound();
    }
}
