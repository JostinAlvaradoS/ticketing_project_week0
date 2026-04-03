using MediatR;
using Waitlist.Application.Ports;
using Waitlist.Application.UseCases.JoinWaitlist;
using Waitlist.Domain.Exceptions;

namespace Waitlist.Api.Endpoints;

public static class WaitlistEndpoints
{
    public static void MapWaitlistEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/waitlist").WithName("Waitlist");

        group.MapPost("/join", JoinWaitlist)
            .WithName("JoinWaitlist")
            .WithSummary("Join the waitlist for a sold-out event");

        group.MapGet("/has-pending", HasPending)
            .WithName("HasPending")
            .WithSummary("Check if email has a pending waitlist entry for an event");
    }

    private static async Task<IResult> JoinWaitlist(
        JoinWaitlistRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new JoinWaitlistCommand(request.Email, request.EventId);
            var result = await mediator.Send(command, cancellationToken);

            return Results.Created($"/api/v1/waitlist/{result.WaitlistEntryId}", new
            {
                waitlistEntryId = result.WaitlistEntryId,
                position = result.Position,
                email = result.Email,
                eventId = result.EventId
            });
        }
        catch (WaitlistConflictException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (WaitlistServiceUnavailableException ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 503);
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> HasPending(
        Guid eventId,
        string email,
        IWaitlistRepository repository,
        CancellationToken cancellationToken)
    {
        var hasPending = await repository.HasActiveEntryAsync(email, eventId, cancellationToken);
        return Results.Ok(new { hasPending });
    }
}

public record JoinWaitlistRequest(string Email, Guid EventId);
