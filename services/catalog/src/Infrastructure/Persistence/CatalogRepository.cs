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

    public async Task<Event?> GetEventWithSeatsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .Include(e => e.Seats)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
    }
}