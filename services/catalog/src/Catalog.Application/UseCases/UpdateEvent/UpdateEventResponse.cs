namespace Catalog.Application.UseCases.UpdateEvent;

public record UpdateEventResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime EventDate,
    string Venue,
    int MaxCapacity,
    decimal BasePrice,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);