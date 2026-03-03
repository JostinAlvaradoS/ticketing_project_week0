namespace Identity.Domain.ValueObjects;

/// <summary>
/// Enumeración que representa los roles disponibles en el sistema.
/// Siguiendo el principio de simplicidad de la constitución: solo Admin y User.
/// </summary>
public enum Role
{
    /// <summary>
    /// Usuario regular con acceso básico al sistema
    /// </summary>
    User = 0,
    
    /// <summary>
    /// Administrador con acceso completo al dashboard administrativo
    /// </summary>
    Admin = 1
}