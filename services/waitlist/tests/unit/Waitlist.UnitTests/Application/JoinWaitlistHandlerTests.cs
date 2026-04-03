using FluentAssertions;
using Moq;
using Waitlist.Application.Ports;
using Waitlist.Domain.Entities;
using Waitlist.Application.UseCases.JoinWaitlist;
using Waitlist.Domain.Exceptions;

namespace Waitlist.UnitTests.Application;

public class JoinWaitlistHandlerTests
{
    private readonly Mock<IWaitlistRepository> _repoMock = new();
    private readonly Mock<ICatalogClient> _catalogMock = new();
    private readonly JoinWaitlistHandler _handler;

    public JoinWaitlistHandlerTests()
    {
        _handler = new JoinWaitlistHandler(_repoMock.Object, _catalogMock.Object);
    }

    // Cycle 7 — Happy path
    [Fact]
    public async Task Handle_ValidEmail_StockZero_CreatesEntryAndReturnsPosition()
    {
        var eventId = Guid.NewGuid();
        var command = new JoinWaitlistCommand("user@example.com", eventId);

        _catalogMock.Setup(c => c.GetAvailableCountAsync(eventId, default)).ReturnsAsync(0);
        _repoMock.Setup(r => r.HasActiveEntryAsync("user@example.com", eventId, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<WaitlistEntry>(), default)).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.GetQueuePositionAsync(It.IsAny<Guid>(), default)).ReturnsAsync(1);

        var result = await _handler.Handle(command, default);

        result.Position.Should().Be(1);
        result.Email.Should().Be("user@example.com");
        result.EventId.Should().Be(eventId);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<WaitlistEntry>(), default), Times.Once);
    }

    // Cycle 8 — Stock available → conflict
    [Fact]
    public async Task Handle_StockAvailable_ThrowsWaitlistConflictException()
    {
        var eventId = Guid.NewGuid();
        _catalogMock.Setup(c => c.GetAvailableCountAsync(eventId, default)).ReturnsAsync(5);

        var act = async () => await _handler.Handle(new JoinWaitlistCommand("user@example.com", eventId), default);

        await act.Should().ThrowAsync<WaitlistConflictException>();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<WaitlistEntry>(), default), Times.Never);
    }

    // Cycle 9 — Duplicate active entry
    [Fact]
    public async Task Handle_DuplicateActiveEntry_ThrowsWaitlistConflictException()
    {
        var eventId = Guid.NewGuid();
        _catalogMock.Setup(c => c.GetAvailableCountAsync(eventId, default)).ReturnsAsync(0);
        _repoMock.Setup(r => r.HasActiveEntryAsync("user@example.com", eventId, default)).ReturnsAsync(true);

        var act = async () => await _handler.Handle(new JoinWaitlistCommand("user@example.com", eventId), default);

        await act.Should().ThrowAsync<WaitlistConflictException>();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<WaitlistEntry>(), default), Times.Never);
    }

    // Cycle 11 — Catalog unavailable
    [Fact]
    public async Task Handle_CatalogClientThrows_ThrowsServiceUnavailableException()
    {
        var eventId = Guid.NewGuid();
        _catalogMock.Setup(c => c.GetAvailableCountAsync(eventId, default))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var act = async () => await _handler.Handle(new JoinWaitlistCommand("user@example.com", eventId), default);

        await act.Should().ThrowAsync<WaitlistServiceUnavailableException>();
    }
}
