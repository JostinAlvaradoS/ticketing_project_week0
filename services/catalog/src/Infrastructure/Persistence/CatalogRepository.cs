using Catalog.Application.Ports;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public class CatalogRepository : ICatalogRepository
{
    private readonly CatalogDbContext _context;

    public CatalogRepository(CatalogDbContext context)  
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync(CancellationToken cancellationToken = default)
    {
        // HUMAN CHECK: Uso de AsNoTracking() para optimización de lectura.
        // Se decide no rastrear entidades en consultas de catálogo para reducir 
        // consumo de memoria y mejorar performance en el listado masivo.
        return await _context.Events
            .AsNoTracking()
            .OrderByDescending(e => e.EventDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Event?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
    }

    public async Task<Event?> GetEventWithSeatsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.Seats)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
    }

    public async Task UpdateSeatStatusAsync(Guid seatId, string status, CancellationToken cancellationToken = default)
    {
        var seat = await _context.Seats.FindAsync(new object[] { seatId }, cancellationToken);
        if (seat != null)
        {
            seat.Status = status;
            _context.Seats.Update(seat);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateSeatStatusAsync(Guid seatId, string status, Guid? reservationId, CancellationToken cancellationToken = default)
    {
        var seat = await _context.Seats.FindAsync(new object[] { seatId }, cancellationToken);
        if (seat != null)
        {
            seat.Status = status;
            if (reservationId.HasValue)
            {
                seat.CurrentReservationId = reservationId;
            }
            _context.Seats.Update(seat);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateSeatStatusByReservationAsync(Guid reservationId, string status, CancellationToken cancellationToken = default)
    {
        var seat = await _context.Seats.FirstOrDefaultAsync(s => s.CurrentReservationId == reservationId, cancellationToken);
        if (seat != null)
        {
            seat.Status = status;
            _context.Seats.Update(seat);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}