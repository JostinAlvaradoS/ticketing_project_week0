using MediatR;
using Inventory.Application.DTOs;
using Inventory.Application.UseCases.CreateReservation;

namespace Inventory.Api.Endpoints;

/// <summary>
/// Endpoints for reservation management in the Inventory service.
/// </summary>
public static class ReservationEndpoints
{
    public static void MapReservationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/reservations")
            .WithName("Reservations");

        group.MapPost("/", CreateReservation)
            .WithName("CreateReservation")
            .WithSummary("Create a new seat reservation")
            .WithDescription("Reserves a seat with a 15-minute TTL. Returns 409 if seat is already reserved.");
    }

    private static async Task<IResult> CreateReservation(
        CreateReservationRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (request.SeatId == Guid.Empty)
            return Results.BadRequest("SeatId must not be empty");

        if (string.IsNullOrEmpty(request.CustomerId))
            return Results.BadRequest("CustomerId must not be empty");

        try
        {
            var command = new CreateReservationCommand(request.SeatId, request.CustomerId, request.EventId ?? Guid.Empty);
            var response = await mediator.Send(command, cancellationToken);
            
            return Results.Created($"/reservations/{response.ReservationId}", response);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    }
}

