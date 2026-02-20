namespace PaymentService.Application.Exceptions;

/// <summary>
/// Excepción lanzada cuando se detecta una violación de unicidad (duplicate key).
/// Abstracción agnóstica de framework para la capa de Application.
/// </summary>
public class DuplicateEntryException : Exception
{
    public DuplicateEntryException(string message) : base(message) { }
    public DuplicateEntryException(string message, Exception innerException) : base(message, innerException) { }
}
