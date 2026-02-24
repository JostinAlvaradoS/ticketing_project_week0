namespace Catalog.Application.UseCases.GetAllEvents;

public record EventDto(
    Guid Id,
    string Name,
    string Description,
    DateTime EventDate,
    decimal BasePrice
);
