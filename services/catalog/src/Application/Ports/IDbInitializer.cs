namespace Catalog.Application.Ports;

/// <summary>
/// Puerto para inicializar la base de datos del contexto de Catalog.
/// Responsable de crear schemas y aplicar migraciones.
/// </summary>
public interface IDbInitializer
{
    /// <summary>
    /// Inicializa la base de datos creando el schema bc_catalog si es necesario
    /// y aplicando todas las migraciones pendientes.
    /// </summary>
    Task InitializeAsync();
}