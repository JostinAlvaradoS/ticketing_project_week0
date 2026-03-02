using Identity.Domain.ValueObjects;

namespace Identity.Application.UseCases.CreateUser;

public record CreateUserCommand(string Email, string Password, Role Role = Role.User);
