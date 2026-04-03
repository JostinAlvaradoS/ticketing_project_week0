using MediatR;
using Microsoft.AspNetCore.Mvc;
using Waitlist.Application.Exceptions;
using Waitlist.Application.Ports;
using Waitlist.Application.UseCases.JoinWaitlist;

namespace Waitlist.Api.Controllers;

[ApiController]
[Route("api/v1/waitlist")]
public class WaitlistController : ControllerBase
{
    private readonly IMediator           _mediator;
    private readonly IWaitlistRepository _repo;

    public WaitlistController(IMediator mediator, IWaitlistRepository repo)
    {
        _mediator = mediator;
        _repo     = repo;
    }

    // POST /api/v1/waitlist/join
    // Spec: US1 — registro en lista de espera
    [HttpPost("join")]
    public async Task<IActionResult> Join([FromBody] JoinRequest request, CancellationToken cancellationToken)
    {
        var validator = new JoinWaitlistCommandValidator();
        var command   = new JoinWaitlistCommand(request.Email, request.EventId);
        var validation = validator.Validate(command);

        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Created($"/api/v1/waitlist/{result.EntryId}", new
            {
                entryId  = result.EntryId,
                position = result.Position
            });
        }
        catch (WaitlistConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (WaitlistServiceUnavailableException ex)
        {
            return StatusCode(503, new { message = ex.Message });
        }
    }

    // GET /api/v1/waitlist/has-pending?eventId={id}
    // Consultado por Inventory.ReservationExpiryWorker (ADR-03)
    [HttpGet("has-pending")]
    public async Task<IActionResult> HasPending([FromQuery] Guid eventId, CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
            return BadRequest(new { message = "eventId is required." });

        var next         = await _repo.GetNextPendingAsync(eventId, cancellationToken);
        var pendingCount = next is not null
            ? await _repo.GetQueuePositionAsync(eventId, cancellationToken)
            : 0;

        return Ok(new { hasPending = next is not null, pendingCount });
    }

    public record JoinRequest(string Email, Guid EventId);
}
