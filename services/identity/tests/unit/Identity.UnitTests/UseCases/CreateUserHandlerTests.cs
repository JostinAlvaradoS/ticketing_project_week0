using Identity.Application.UseCases.CreateUser;
using Identity.Domain.Entities;
using Identity.Domain.Ports;
using Moq;
using FluentAssertions;

namespace Identity.UnitTests.UseCases;

public class CreateUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly CreateUserHandler _handler;

    public CreateUserHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _handler = new CreateUserHandler(_userRepositoryMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_WithNewUser_ShouldCreateUserAndReturnId()
    {
        // Arrange
        var command = new CreateUserCommand("test@example.com", "Password123!");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email)).ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(x => x.HashPassword(command.Password)).Returns("hashed-password");

        // Act
        var result = await _handler.Handle(command);

        // Assert
        result.Should().NotBeEmpty();
        _userRepositoryMock.Verify(x => x.SaveAsync(It.Is<User>(u => u.Email == command.Email && u.PasswordHash == "hashed-password")), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingUser_ShouldThrowException()
    {
        // Arrange
        var command = new CreateUserCommand("existing@example.com", "Password123!");
        var existingUser = new User(command.Email, "some-hash");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email)).ReturnsAsync(existingUser);

        // Act
        var act = () => _handler.Handle(command);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage($"*already exists*");
    }
}
