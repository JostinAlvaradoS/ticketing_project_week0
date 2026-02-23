namespace Identity.Application.UseCases.CreateUser;

using Identity.Domain.Ports;
using Identity.Domain.Entities;

public class CreateUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(CreateUserCommand command)
    {
        // Verificar que el usuario no existe
        var existingUser = await _userRepository.GetByEmailAsync(command.Email);
        if (existingUser is not null)
            throw new Exception($"User with email {command.Email} already exists");

        // Hash la contraseña
        var passwordHash = _passwordHasher.HashPassword(command.Password);

        // Crear usuario
        var user = new User(command.Email, passwordHash);

        // Guardar
        await _userRepository.SaveAsync(user);

        return user.Id;
    }
}
