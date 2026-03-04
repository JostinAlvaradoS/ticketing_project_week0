using MediatR;

namespace Catalog.Application.UseCases.GenerateSeats;

public record SeatSectionConfiguration(
    string SectionCode,
    int Rows,
    int SeatsPerRow,
    decimal PriceMultiplier);

public record GenerateSeatsCommand(
    Guid EventId,
    List<SeatSectionConfiguration> SectionConfigurations) : IRequest<GenerateSeatsResponse>;