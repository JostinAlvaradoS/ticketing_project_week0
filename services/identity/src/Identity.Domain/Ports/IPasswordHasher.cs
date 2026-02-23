namespace Identity.Domain.Ports;

/// <summary>
/// Puerto para hash y validación de passwords.
/// Abstrae los detalles de implementación del algoritmo de hashing.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Genera un hash de la contraseña proporcionada.
    /// </summary>
    /// <param name="password">Contraseña en texto plano</param>
    /// <returns>Hash seguro de la contraseña</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifica que una contraseña en texto plano coincide con un hash almacenado.
    /// </summary>
    /// <param name="password">Contraseña en texto plano</param>
    /// <param name="hash">Hash almacenado</param>
    /// <returns>True si la contraseña coincide con el hash, false en caso contrario</returns>
    bool VerifyPassword(string password, string hash);
}
