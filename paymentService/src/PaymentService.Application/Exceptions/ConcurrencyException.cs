namespace PaymentService.Application.Exceptions;

/// <summary>
/// Excepción lanzada cuando ocurre un conflicto de concurrencia en la persistencia.
/// Abstracción agnóstica de framework para la capa de Application.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
    public ConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
}
