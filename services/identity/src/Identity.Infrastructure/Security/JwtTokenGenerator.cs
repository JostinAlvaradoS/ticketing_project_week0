using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Identity.Domain.Entities;
using Identity.Application.Ports;
using Microsoft.Extensions.Configuration;

namespace Identity.Infrastructure.Security;

public class JwtTokenGenerator : ITokenGenerator
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]!;
        _issuer = configuration["Jwt:Issuer"]!;
        _audience = configuration["Jwt:Audience"]!;
    }

    public string Generate(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_key);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            // Agregar claim de rol para autenticación frontend
            new Claim("role", user.Role.ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }
}