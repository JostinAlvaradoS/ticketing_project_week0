namespace Identity.Application.UseCases.IssueToken;

using Identity.Domain.Ports;
using Identity.Domain.Entities;
public class IssueTokenHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenGenerator _tokenGenerator;

    public IssueTokenHandler(
        IUserRepository userRepository,
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<TokenResult> Handle(IssueTokenCommand command)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email);

        if (user is null)
            throw new Exception("User not found");

        var expiresAt = DateTime.UtcNow.AddHours(2); // Debe coincidir con JwtTokenGenerator
        var token = _tokenGenerator.Generate(user);

        return new TokenResult(token, expiresAt);
    }
}