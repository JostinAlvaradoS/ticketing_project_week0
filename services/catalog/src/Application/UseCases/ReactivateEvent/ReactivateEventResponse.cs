namespace Catalog.Application.UseCases.ReactivateEvent;

public record ReactivateEventResponse(
    Guid Id,
    string Name,
    string Status,
    DateTime? UpdatedAt,
    bool Success);