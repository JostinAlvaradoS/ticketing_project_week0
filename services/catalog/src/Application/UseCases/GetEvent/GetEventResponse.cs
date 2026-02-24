namespace Catalog.Application.UseCases.GetEvent;

public record GetEventResponse(
    Guid Id,
    string Name,
    string Description,
    DateTime EventDate,
    decimal BasePrice
);
