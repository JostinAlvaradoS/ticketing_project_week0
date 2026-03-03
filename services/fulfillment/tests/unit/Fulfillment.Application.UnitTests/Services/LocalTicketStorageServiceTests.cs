using Fulfillment.Application.Ports;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.PdfGeneration;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Application.UnitTests.Services;

public class LocalTicketStorageServiceTests
{
    private string _testStoragePath;

    public LocalTicketStorageServiceTests()
    {
        _testStoragePath = Path.Combine(Path.GetTempPath(), "ticket_tests");
        if (Directory.Exists(_testStoragePath))
        {
            Directory.Delete(_testStoragePath, true);
        }
    }

    [Fact]
    public async Task SaveTicketPdfAsync_With_Valid_Stream_Should_Save_File()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LocalTicketStorageService>>();
        var ticketId = Guid.NewGuid();
        var pdfContent = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
        var pdfStream = new MemoryStream(pdfContent);

        var service = new LocalTicketStorageService(loggerMock.Object);

        // Act
        var relativePath = await service.SaveTicketPdfAsync(ticketId, pdfStream);

        // Assert
        relativePath.Should().NotBeNullOrEmpty();
        relativePath.Should().Contain(ticketId.ToString());
        relativePath.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task SaveTicketPdfAsync_Should_Create_Storage_Directory_If_Not_Exists()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LocalTicketStorageService>>();
        var ticketId = Guid.NewGuid();
        var pdfStream = new MemoryStream(new byte[] { });

        var service = new LocalTicketStorageService(loggerMock.Object);

        // Act
        var relativePath = await service.SaveTicketPdfAsync(ticketId, pdfStream);

        // Assert
        relativePath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SaveTicketPdfAsync_Multiple_Saves_Should_Generate_Different_Filenames()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LocalTicketStorageService>>();
        var ticketId1 = Guid.NewGuid();
        var ticketId2 = Guid.NewGuid();
        
        var pdfStream1 = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        var pdfStream2 = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        var service = new LocalTicketStorageService(loggerMock.Object);

        // Act
        var path1 = await service.SaveTicketPdfAsync(ticketId1, pdfStream1);
        var path2 = await service.SaveTicketPdfAsync(ticketId2, pdfStream2);

        // Assert
        path1.Should().NotBeNull();
        path2.Should().NotBeNull();
        path1.Should().NotBe(path2);
    }

    [Fact]
    public async Task SaveTicketPdfAsync_Should_Return_Relative_Path()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<LocalTicketStorageService>>();
        var ticketId = Guid.NewGuid();
        var pdfStream = new MemoryStream(new byte[] { });

        var service = new LocalTicketStorageService(loggerMock.Object);

        // Act
        var relativePath = await service.SaveTicketPdfAsync(ticketId, pdfStream);

        // Assert
        relativePath.Should().StartWith("tickets");
        relativePath.Should().NotStartWith("/");
        relativePath.Should().NotStartWith("\\");
    }
}
