using Microsoft.Extensions.Logging;
using ReservationService.Application.Dtos;
using ReservationService.Application.Ports.Inbound;
using ReservationService.Application.Ports.Outbound;
using ReservationService.Domain.Enums;

namespace ReservationService.Application.UseCases;

public class ReserveTicketUseCase : IReserveTicketUseCase
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<ReserveTicketUseCase> _logger;

    public ReserveTicketUseCase(ITicketRepository ticketRepository, ILogger<ReserveTicketUseCase> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
    }

    public async Task<ReservationResult> ExecuteAsync(ReservationMessageDto message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing reservation for Ticket {TicketId}, Order {OrderId}, User {ReservedBy}",
            message.TicketId, message.OrderId, message.ReservedBy);

        var ticket = await _ticketRepository.GetByIdAsync(message.TicketId, cancellationToken);

        if (ticket is null)
        {
            _logger.LogWarning("Ticket {TicketId} not found", message.TicketId);
            return new ReservationResult(false, $"Ticket {message.TicketId} not found");
        }

        if (ticket.Status != TicketStatus.Available)
        {
            _logger.LogWarning(
                "Ticket {TicketId} is not available. Current status: {Status}",
                message.TicketId, ticket.Status);
            return new ReservationResult(false, $"Ticket {message.TicketId} is not available (status: {ticket.Status})");
        }

        var expiresAt = DateTime.UtcNow.AddSeconds(message.ReservationDurationSeconds);

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
