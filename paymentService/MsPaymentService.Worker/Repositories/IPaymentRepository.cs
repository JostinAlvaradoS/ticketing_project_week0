using MsPaymentService.Worker.Models.Entities;

namespace MsPaymentService.Worker.Repositories;

/// <summary>
/// Contrato para el acceso a datos de pagos.
/// Define operaciones CRUD sobre la tabla de pagos.
/// </summary>
public interface IPaymentRepository
{
    /// <summary>
    /// Obtiene el pago asociado a un ticket específico.
    /// </summary>
    /// <param name="ticketId">Identificador del ticket.</param>
    /// <returns>El pago encontrado o null si no existe.</returns>
    Task<Payment?> GetByTicketIdAsync(long ticketId);

    /// <summary>
    /// Obtiene un pago por su identificador, incluyendo el ticket asociado.
    /// </summary>
    /// <param name="id">Identificador del pago.</param>
    /// <returns>El pago encontrado o null si no existe.</returns>
    Task<Payment?> GetByIdAsync(long id);

    Task AddAsync(Payment payment, CancellationToken ct);

    Task<Payment?> GetByProviderRefAsync(string providerRef, CancellationToken ct);

}