namespace Catalog.Application.UseCases.GetEventSeatmap;

public record GetEventSeatmapResponse(
    Guid EventId,
    string EventName,
    string EventDescription,
    DateTime EventDate,
    decimal BasePrice,
    IEnumerable<SeatDto> Seats
);

public record SeatDto(
    Guid Id,
    string SectionCode,
    int RowNumber,  
    int SeatNumber,
    decimal Price,
    string Status
);