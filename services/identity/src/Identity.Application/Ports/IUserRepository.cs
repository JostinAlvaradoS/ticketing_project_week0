using Identity.Domain.Entities;

namespace Identity.Application.Ports;

/// <summary>
/// Puerto de persistencia para la entidad User.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task SaveAsync(User user);
}
