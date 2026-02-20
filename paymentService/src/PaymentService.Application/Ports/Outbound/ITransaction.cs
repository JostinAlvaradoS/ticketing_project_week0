namespace PaymentService.Application.Ports.Outbound;

/// <summary>
/// Abstracción de transacción de base de datos para la capa de Application.
/// No depende de ningún framework ORM específico.
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
