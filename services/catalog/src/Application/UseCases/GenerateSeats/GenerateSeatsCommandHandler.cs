using Catalog.Application.Ports;
using Catalog.Domain.Entities;
using MediatR;

namespace Catalog.Application.UseCases.GenerateSeats;

public class GenerateSeatsCommandHandler : IRequestHandler<GenerateSeatsCommand, GenerateSeatsResponse>
{
    private readonly ICatalogRepository _catalogRepository;

    public GenerateSeatsCommandHandler(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<GenerateSeatsResponse> Handle(GenerateSeatsCommand request, CancellationToken cancellationToken)
    {
        // Validate the event exists - Following Gherkin: "Error al generar asientos para evento inexistente"
        var eventEntity = await _catalogRepository.GetEventAsync(request.EventId, cancellationToken);
        if (eventEntity == null)
        {
            throw new InvalidOperationException("Evento no encontrado");
        }

        // Generate seats for each section configuration
        var allSeats = new List<Seat>();
        var sectionSummaries = new List<SeatGenerationSummary>();

        foreach (var sectionConfig in request.SectionConfigurations)
        {
            var sectionSeats = GenerateSeatsForSection(
                request.EventId,
                sectionConfig,
                eventEntity.BasePrice);
            
            allSeats.AddRange(sectionSeats);
            
            // Calculate section summary
            var seatPrice = eventEntity.BasePrice * sectionConfig.PriceMultiplier;
            var sectionSummary = new SeatGenerationSummary(
                sectionConfig.SectionCode,
                sectionSeats.Count,
                seatPrice);
            
            sectionSummaries.Add(sectionSummary);
        }

        // Validate against event capacity - Following Gherkin: "Validar capacidad máxima vs asientos generados"
        eventEntity.ValidateSeatCapacity(allSeats.Count);

        // Persist all seats
        await _catalogRepository.AddSeatsAsync(allSeats, cancellationToken);
        await _catalogRepository.SaveChangesAsync(cancellationToken);

        return new GenerateSeatsResponse(
            request.EventId,
            allSeats.Count,
            sectionSummaries);
    }

    private static List<Seat> GenerateSeatsForSection(
        Guid eventId, 
        SeatSectionConfiguration sectionConfig, 
        decimal basePrice)
    {
        var seats = new List<Seat>();
        var seatPrice = basePrice * sectionConfig.PriceMultiplier;

        // Generate seats: rows × seats_per_row - Following Gherkin numbering
        for (int row = 1; row <= sectionConfig.Rows; row++)
        {
            for (int seatNumber = 1; seatNumber <= sectionConfig.SeatsPerRow; seatNumber++)
            {
                var seat = new Seat
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    SectionCode = sectionConfig.SectionCode,
                    RowNumber = row,
                    SeatNumber = seatNumber,
                    Price = seatPrice,
                    Status = Seat.StatusAvailable // Following Gherkin: "todos los asientos tienen estado available"
                };

                // Validate the seat before adding
                seat.ValidateBusinessRules();
                seats.Add(seat);
            }
        }

        return seats;
    }
}