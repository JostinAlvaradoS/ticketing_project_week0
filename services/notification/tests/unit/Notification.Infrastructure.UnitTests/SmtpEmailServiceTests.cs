using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Infrastructure.Email;
using FluentAssertions;
using System.Threading.Tasks;

namespace Notification.Infrastructure.UnitTests.Email;

public class SmtpEmailServiceTests
{
    private readonly Mock<ILogger<SmtpEmailService>> _loggerMock;
    private readonly SmtpEmailOptions _options;

    public SmtpEmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<SmtpEmailService>>();
        _options = new SmtpEmailOptions();
    }

    [Fact]
    public async Task SendAsync_InDevMode_ReturnsTrue()
    {
        // Arrange
        _options.UseDevMode = true;
        var optionsMock = Options.Create(_options);
        var service = new SmtpEmailService(optionsMock, _loggerMock.Object);

        // Act
        var result = await service.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_InProdMode_ReturnsTrue()
    {
        // Arrange
        _options.UseDevMode = false;
        var optionsMock = Options.Create(_options);
        var service = new SmtpEmailService(optionsMock, _loggerMock.Object);

        // Act
        var result = await service.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        result.Should().BeTrue();
    }
}
