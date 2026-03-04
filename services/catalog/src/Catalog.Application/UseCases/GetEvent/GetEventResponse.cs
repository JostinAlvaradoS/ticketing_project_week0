namespace Catalog.Application.UseCases.GetEvent;

public record GetEventResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime EventDate,
    string Venue,
    int MaxCapacity,
    decimal BasePrice,
    bool IsActive,
    int TotalSeats,
    int AvailableSeats,
    int ReservedSeats,
    int SoldSeats,
    decimal Revenue
);
