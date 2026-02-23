using Microsoft.AspNetCore.Mvc;
using MediatR;
using Catalog.Application.UseCases.GetEventSeatmap;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EventsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}/seatmap")]
    public async Task<IActionResult> GetEventSeatmap(Guid id)
    {
        var query = new GetEventSeatmapQuery(id);
        var result = await _mediator.Send(query);
        
        return result is not null ? Ok(result) : NotFound();
    }
}