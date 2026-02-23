namespace Identity.Domain.Ports;
using Identity.Domain.Entities;


public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
}