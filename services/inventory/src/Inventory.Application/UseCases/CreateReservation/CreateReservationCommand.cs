using MediatR;
using Inventory.Application.DTOs;

namespace Inventory.Application.UseCases.CreateReservation;

/// <summary>
/// Command to create a seat reservation with Redis lock and optimistic concurrency control.
/// </summary>
public record CreateReservationCommand(
    Guid SeatId,
    string CustomerId
) : IRequest<CreateReservationResponse>;
