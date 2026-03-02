using FluentAssertions;
using Identity.Domain.Entities;
using Xunit;

namespace Identity.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_Should_Initialize_User_Correctly()
    {
        // Arrange
        var email = "test@example.com";
        var passwordHash = "hashed_password";

        // Act
        var user = new User(email, passwordHash);

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
    }
}
