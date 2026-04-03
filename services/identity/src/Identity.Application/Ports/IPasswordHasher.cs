namespace Identity.Application.Ports;

/// <summary>
/// Puerto para hash y validación de passwords.
/// Abstrae los detalles de implementación del algoritmo de hashing.
/// </summary>
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
