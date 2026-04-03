using Identity.Domain.Entities;

namespace Identity.Application.Ports;

/// <summary>
/// Puerto para generación de tokens de autenticación (JWT).
/// </summary>
public interface ITokenGenerator
{
    string Generate(User user);
}
