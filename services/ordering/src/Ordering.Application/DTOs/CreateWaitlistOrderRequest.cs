namespace Ordering.Application.DTOs;

public record CreateWaitlistOrderRequest(Guid SeatId, string GuestToken, decimal Price);
