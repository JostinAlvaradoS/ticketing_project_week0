namespace Ordering.Application.DTOs;

/// <summary>
/// DTO de enriquecimiento de Order utilizado por el servicio Fulfillment para generar tickets.
/// </summary>
public record OrderEnrichmentDto(
    Guid OrderId,
    string? CustomerEmail,
    Guid? EventId,
    string? EventName,
    string? SeatNumber,
    decimal Price,
    string Currency
);
