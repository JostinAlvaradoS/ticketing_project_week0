namespace Fulfillment.Application.Ports;

public interface IOrderingServiceClient
{
    Task<OrderDetailsDto?> GetOrderDetailsAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public record OrderDetailsDto(
    Guid OrderId,
    string CustomerEmail,
    Guid? EventId,
    string? EventName,
    string SeatNumber,
    decimal Price,
    string Currency
);
