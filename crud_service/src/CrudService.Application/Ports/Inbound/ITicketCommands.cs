using CrudService.Application.Dtos;

namespace CrudService.Application.Ports.Inbound;

public interface ITicketCommands
{
    Task<IEnumerable<TicketDto>> CreateTicketsAsync(long eventId, int quantity);
    Task<TicketDto> UpdateTicketStatusAsync(long id, string newStatus, string? reason = null);
    Task<TicketDto> ReleaseTicketAsync(long id, string? reason = null);
}
