namespace Ordering.Application.Ports;

/// <summary>
/// Puerto para inicializar la base de datos del contexto de Ordering.
/// Responsable de crear schemas y aplicar migraciones.
/// </summary>
public interface IDbInitializer
{
    /// <summary>
    /// Inicializa la base de datos creando el schema bc_ordering si es necesario
    /// y aplicando todas las migraciones pendientes.
    /// </summary>
    Task InitializeAsync();
}