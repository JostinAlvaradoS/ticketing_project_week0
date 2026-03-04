namespace Catalog.Application.UseCases.GetAllEvents;

public record EventDto(
    Guid Id,
    string Name,
    string Description,
    DateTime EventDate,
    string Venue,
    int MaxCapacity,
    decimal BasePrice,
    bool IsActive,
    int TotalSeats,
    int SoldSeats,
    decimal Revenue
);
