using ReservationService.Worker.Models;
using ReservationService.Worker.Repositories;

namespace ReservationService.Worker.Services;

public class ReservationServiceImpl : IReservationService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<ReservationServiceImpl> _logger;

    public ReservationServiceImpl(ITicketRepository ticketRepository, ILogger<ReservationServiceImpl> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    // 🛡 HUMAN CHECK:
    // Reservation logic validates that the ticket exists and is available first.
    // If the ticket was already reserved by another process, it is silently rejected
    // (not an error, it's an expected scenario under high concurrency).
    public async Task<ReservationResult> ProcessReservationAsync(
        ReservationMessage message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing reservation for Ticket {TicketId}, Order {OrderId}, User {ReservedBy}",
            message.TicketId, message.OrderId, message.ReservedBy);

        // 1. Find the ticket
        var ticket = await _ticketRepository.GetByIdAsync(message.TicketId, cancellationToken);

        if (ticket is null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", message.TicketId);
            return new ReservationResult(false, $"Ticket {message.TicketId} not found");
        }

        // 2. Validate it's available
        if (ticket.Status != TicketStatus.Available)
        {
            _logger.LogWarning(
                "Ticket {TicketId} is not available. Current status: {Status}",
                message.TicketId, ticket.Status);
            return new ReservationResult(false, $"Ticket {message.TicketId} is not available (status: {ticket.Status})");
        }

        // 3. Calculate expiration time
        var expiresAt = DateTime.UtcNow.AddSeconds(message.ReservationDurationSeconds);

        // 4. Try to reserve (with optimistic locking)
        var reserved = await _ticketRepository.TryReserveAsync(
            ticket,
            message.ReservedBy,
            message.OrderId,
            expiresAt,
            cancellationToken);

        if (!reserved)
        {
            return new ReservationResult(false, "Ticket was modified by another process");
        }

        _logger.LogInformation(
            "Reservation completed for Ticket {TicketId}. Expires at {ExpiresAt}",
            message.TicketId, expiresAt);

        return new ReservationResult(true);
    }
}
