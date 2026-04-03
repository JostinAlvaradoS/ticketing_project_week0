namespace Identity.Application.UseCases.IssueToken;

using Identity.Application.Ports;
using Identity.Domain.Entities;

public class IssueTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public IssueTokenHandler(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<TokenResult> Handle(IssueTokenCommand command)
    {
        // Buscar usuario por email
        var user = await _userRepository.GetByEmailAsync(command.Email);

        if (user is null)
            throw new Exception("Invalid credentials");

        // Validar que la contraseña coincide con el hash almacenado
        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
            throw new Exception("Invalid credentials");

        // Generar token
        var expiresAt = DateTime.UtcNow.AddHours(2);
        var token = _tokenGenerator.Generate(user);

        return new TokenResult(token, expiresAt, user.Role, user.Email);
    }
}