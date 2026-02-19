using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

/// <summary>
/// Contrato para el acceso a datos de tickets.
/// Define operaciones de lectura y escritura sobre la tabla de tickets.
/// </summary>
public interface ITicketRepository
{
    /// <summary>
    /// Obtiene un ticket por su identificador, incluyendo los pagos asociados.
    /// </summary>
    /// <param name="id">Identificador del ticket.</param>
    /// <returns>El ticket encontrado o null si no existe.</returns>
    Task<Ticket?> GetByIdAsync(long id);

    /// <summary>
    /// Obtiene todos los tickets reservados cuya reserva ha expirado.
    /// </summary>
    /// <param name="expirationThreshold">Fecha límite de expiración (UTC).</param>
    /// <returns>Lista de tickets con reservas expiradas.</returns>
    Task<List<Ticket>> GetExpiredReservedTicketsAsync(DateTime expirationThreshold);

    Task<Ticket?> GetTrackedByIdAsync(long id, CancellationToken ct);
}