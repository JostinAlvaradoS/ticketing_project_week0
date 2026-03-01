namespace Catalog.Application.UseCases.GenerateSeats;

public record SeatGenerationSummary(
    string SectionCode,
    int SeatsGenerated,
    decimal SeatPrice);

public record GenerateSeatsResponse(
    Guid EventId,
    int TotalSeatsGenerated,
    List<SeatGenerationSummary> SectionSummaries);