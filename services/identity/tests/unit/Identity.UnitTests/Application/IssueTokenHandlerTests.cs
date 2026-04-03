using FluentAssertions;
using Identity.Application.UseCases.IssueToken;
using Identity.Domain.Entities;
using Identity.Application.Ports;
using Moq;
using Xunit;

namespace Identity.UnitTests.Application;

public class IssueTokenHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly IssueTokenHandler _handler;

    public IssueTokenHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _handler = new IssueTokenHandler(
            _userRepositoryMock.Object,
            _tokenGeneratorMock.Object,
            _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var command = new IssueTokenCommand("test@example.com", "password123");
        var user = new User(command.Email, "hashed_password");
        
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(h => h.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);
            
        _tokenGeneratorMock.Setup(g => g.Generate(user))
            .Returns("valid_token");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("valid_token");
        _tokenGeneratorMock.Verify(g => g.Generate(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidUser_ShouldThrowException()
    {
        // Arrange
        var command = new IssueTokenCommand("nonexistent@example.com", "password");
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command));
        exception.Message.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldThrowException()
    {
        // Arrange
        var command = new IssueTokenCommand("test@example.com", "wrong_password");
        var user = new User(command.Email, "hashed_password");
        
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(command.Email))
            .ReturnsAsync(user);
        
        _passwordHasherMock.Setup(h => h.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command));
        exception.Message.Should().Be("Invalid credentials");
    }
}
