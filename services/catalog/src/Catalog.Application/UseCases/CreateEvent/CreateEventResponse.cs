namespace Catalog.Application.UseCases.CreateEvent;

public record CreateEventResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime EventDate,
    string Venue,
    int MaxCapacity,
    decimal BasePrice,
    string Status,
    DateTime CreatedAt);