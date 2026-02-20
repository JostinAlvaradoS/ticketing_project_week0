using CrudService.Application.Dtos;

namespace CrudService.Application.Ports.Inbound;

public interface ITicketQueries
{
    Task<IEnumerable<TicketDto>> GetTicketsByEventAsync(long eventId);
    Task<TicketDto?> GetTicketByIdAsync(long id);
    Task<IEnumerable<TicketDto>> GetExpiredTicketsAsync();
}
