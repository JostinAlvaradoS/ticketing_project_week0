using Identity.Domain.Ports;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Implementación de IPasswordHasher usando BCrypt.
/// Proporciona hash seguro y validación de contraseñas.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Genera un hash BCrypt de la contraseña con salt autogenerado.
    /// </summary>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <summary>
    /// Verifica que la contraseña coincide con el hash usando BCrypt.
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
