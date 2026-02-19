using Producer.Application.Dtos;
using Producer.Application.Ports.Inbound;
using Producer.Application.Ports.Outbound;
using Producer.Domain.Events;

namespace Producer.Application.UseCases;

public class ReserveTicketUseCase : IReserveTicketUseCase
{
    private readonly ITicketEventPublisher _ticketPublisher;

    public ReserveTicketUseCase(ITicketEventPublisher ticketPublisher)
    {
        _ticketPublisher = ticketPublisher;
    }

    public async Task<ReserveTicketResult> ExecuteAsync(ReserveTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EventId <= 0)
            return new ReserveTicketResult { Success = false, Message = "EventId debe ser mayor a 0" };

        if (request.TicketId <= 0)
            return new ReserveTicketResult { Success = false, Message = "TicketId debe ser mayor a 0" };

        if (string.IsNullOrWhiteSpace(request.OrderId))
            return new ReserveTicketResult { Success = false, Message = "OrderId es requerido" };

        if (string.IsNullOrWhiteSpace(request.ReservedBy))
            return new ReserveTicketResult { Success = false, Message = "ReservedBy es requerido" };

        if (request.ExpiresInSeconds <= 0)
            return new ReserveTicketResult { Success = false, Message = "ExpiresInSeconds debe ser mayor a 0" };

        var ticketEvent = new TicketReservedEvent
        {
            TicketId = request.TicketId,
            EventId = request.EventId,
            OrderId = request.OrderId,
            ReservedBy = request.ReservedBy,
            ReservationDurationSeconds = request.ExpiresInSeconds,
            PublishedAt = DateTime.UtcNow
        };

        await _ticketPublisher.PublishAsync(ticketEvent, cancellationToken);

        return new ReserveTicketResult
        {
            Success = true,
            TicketId = request.TicketId,
            Message = "Reserva procesada"
        };
    }
}
