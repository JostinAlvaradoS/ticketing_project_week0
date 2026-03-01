using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TicketingFlow.IntegrationTests.Infrastructure;

/// <summary>
/// Helper class for generating JWT tokens for integration testing.
/// Mimics the same JWT configuration as used in services.
/// </summary>
public static class JwtTestHelper
{
    private const string TestJwtKey = "dev-secret-key-minimum-32-chars-required-for-security";
    private const string TestIssuer = "SpecKit.Identity";
    private const string TestAudience = "SpecKit.Services";

    /// <summary>
    /// Generate a JWT token for an admin user for testing admin endpoints.
    /// </summary>
    public static string GenerateAdminToken()
    {
        return GenerateToken("admin@test.com", "test-admin-123");
    }

    /// <summary>
    /// Generate a JWT token for a regular customer user.
    /// </summary>
    public static string GenerateCustomerToken()
    {
        return GenerateToken("customer@test.com", "test-customer-456");
    }

    /// <summary>
    /// Generate a JWT token with custom claims.
    /// </summary>
    private static string GenerateToken(string email, string userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestJwtKey);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }
}