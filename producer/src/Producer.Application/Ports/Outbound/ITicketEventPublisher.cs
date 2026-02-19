using Producer.Domain.Events;

namespace Producer.Application.Ports.Outbound;

public interface ITicketEventPublisher
{
    Task PublishAsync(TicketReservedEvent ticketEvent, CancellationToken cancellationToken = default);
}
