namespace Identity.Domain.Ports;

/// <summary>
/// Puerto para inicializar la base de datos (aplicar migraciones).
/// Abstrae los detalles de implementación de la infraestructura.
/// </summary>
public interface IDbInitializer
{
    /// <summary>
    /// Aplica las migraciones pendientes a la base de datos.
    /// </summary>
    Task InitializeAsync();
}
