namespace Producer.Application.Ports.Inbound;

using Producer.Application.Dtos;

public interface IReserveTicketUseCase
{
    Task<ReserveTicketResult> ExecuteAsync(ReserveTicketRequest request, CancellationToken cancellationToken = default);
}
