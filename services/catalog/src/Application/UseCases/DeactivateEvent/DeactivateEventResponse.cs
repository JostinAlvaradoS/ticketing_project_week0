namespace Catalog.Application.UseCases.DeactivateEvent;

public record DeactivateEventResponse(
    Guid Id,
    string Name,
    string Status,
    DateTime? UpdatedAt,
    bool Success);