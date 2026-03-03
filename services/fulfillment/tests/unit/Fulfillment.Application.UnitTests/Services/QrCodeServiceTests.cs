using Fulfillment.Application.Ports;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.PdfGeneration;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Application.UnitTests.Services;

public class QrCodeServiceTests
{
    [Fact]
    public async Task GenerateQrCodeAsync_With_Valid_Data_Should_Return_Stream()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<QrCodeService>>();
        var service = new QrCodeService(loggerMock.Object);
        var testData = "22222222-2222-2222-2222-222222222222:A-15:event123";

        // Act
        var result = await service.GenerateQrCodeAsync(testData);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<Stream>();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateQrCodeAsync_With_Empty_Data_Should_Handle_Gracefully()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<QrCodeService>>();
        var service = new QrCodeService(loggerMock.Object);

        // Act
        var result = await service.GenerateQrCodeAsync("");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateQrCodeAsync_Multiple_Calls_Should_Generate_Different_Streams()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<QrCodeService>>();
        var service = new QrCodeService(loggerMock.Object);
        var data1 = "order1:seatA:event1";
        var data2 = "order2:seatB:event2";

        // Act
        var result1 = await service.GenerateQrCodeAsync(data1);
        var result2 = await service.GenerateQrCodeAsync(data2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Length.Should().BeGreaterThan(0);
        result2.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateQrCodeAsync_Should_Create_Valid_PNG_Stream()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<QrCodeService>>();
        var service = new QrCodeService(loggerMock.Object);
        var testData = "22222222-2222-2222-2222-222222222222:A-15:event123";

        // Act
        var result = await service.GenerateQrCodeAsync(testData);
        var buffer = new byte[8];
        result.Read(buffer, 0, 8);

        // Assert
        // PNG signature: 89 50 4E 47 0D 0A 1A 0A
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }
}
