namespace Identity.Domain.Ports;
using Identity.Domain.Entities;

public interface ITokenGenerator
{
    string Generate(User user);
}