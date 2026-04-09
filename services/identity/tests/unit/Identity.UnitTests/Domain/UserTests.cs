using FluentAssertions;
using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Xunit;

namespace Identity.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void Constructor_Should_Initialize_User_Correctly()
    {
        var user = new User("test@example.com", "hashed_password");

        user.Id.Should().NotBeEmpty();
        user.Email.Should().Be("test@example.com");
        user.PasswordHash.Should().Be("hashed_password");
        user.Role.Should().Be(Role.User);
    }

    [Fact]
    public void Constructor_WithExplicitAdminRole_ShouldAssignAdminRole()
    {
        var user = new User("admin@example.com", "hashed_password", Role.Admin);

        user.Role.Should().Be(Role.Admin);
    }

    [Fact]
    public void IsAdmin_WhenRoleIsUser_ReturnsFalse()
    {
        var user = new User("user@example.com", "hash");

        user.IsAdmin().Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_WhenRoleIsAdmin_ReturnsTrue()
    {
        var user = new User("admin@example.com", "hash", Role.Admin);

        user.IsAdmin().Should().BeTrue();
    }

    [Fact]
    public void GrantAdminRole_ShouldChangeRoleToAdmin()
    {
        var user = new User("user@example.com", "hash");

        user.GrantAdminRole();

        user.Role.Should().Be(Role.Admin);
        user.IsAdmin().Should().BeTrue();
    }

    [Fact]
    public void RevokeAdminRole_ShouldChangeRoleToUser()
    {
        var user = new User("admin@example.com", "hash", Role.Admin);

        user.RevokeAdminRole();

        user.Role.Should().Be(Role.User);
        user.IsAdmin().Should().BeFalse();
    }
}
