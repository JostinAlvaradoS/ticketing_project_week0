using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReservationService.Application.Ports.Outbound;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;
using ReservationService.Infrastructure.Persistence;
using System.Data;

namespace ReservationService.Infrastructure.Persistence;

public class TicketRepository : ITicketRepository
{
    private readonly TicketingDbContext _context;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(TicketingDbContext context, ILogger<TicketRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Ticket?> GetByIdAsync(long ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets.FindAsync([ticketId], cancellationToken);
    }

    public async Task<bool> TryReserveAsync(
        Ticket ticket,
        string reservedBy,
        string orderId,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var currentVersion = ticket.Version;

        try
        {
            var sql = @"
                UPDATE tickets 
                SET status = 'reserved',
                    reserved_by = {0},
                    order_id = {1},
                    reserved_at = {2},
                    expires_at = {3},
                    version = version + 1
                WHERE id = {4} 
                  AND version = {5} 
                  AND status = 'available'";

            var affected = await _context.Database.ExecuteSqlRawAsync(
                sql,
                new object[] { reservedBy, orderId, DateTime.UtcNow, expiresAt, ticket.Id, currentVersion },
                cancellationToken);

            if (affected == 0)
            {
                _logger.LogWarning(
                    "Failed to reserve ticket {TicketId}: concurrent modification or not available",
                    ticket.Id);
                return false;
            }

            _logger.LogInformation(
                "Ticket {TicketId} reserved successfully for {ReservedBy} until {ExpiresAt}",
                ticket.Id, reservedBy, expiresAt);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving ticket {TicketId}", ticket.Id);
            return false;
        }
    }
}
