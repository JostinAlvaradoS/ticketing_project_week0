using ReservationService.Application.Dtos;

namespace ReservationService.Application.Ports.Inbound;

public interface IReserveTicketUseCase
{
    Task<ReservationResult> ExecuteAsync(ReservationMessageDto message, CancellationToken cancellationToken = default);
}

public record ReservationResult(bool Success, string? ErrorMessage = null);
