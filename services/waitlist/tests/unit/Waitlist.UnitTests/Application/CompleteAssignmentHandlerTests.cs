using FluentAssertions;
using Moq;
using Waitlist.Application.Ports;
using Waitlist.Application.UseCases.CompleteAssignment;
using Waitlist.Domain.Entities;

namespace Waitlist.UnitTests.Application;

public class CompleteAssignmentHandlerTests
{
    private readonly Mock<IWaitlistRepository> _repoMock = new();
    private readonly CompleteAssignmentHandler _handler;

    public CompleteAssignmentHandlerTests()
    {
        _handler = new CompleteAssignmentHandler(_repoMock.Object);
    }

    // Cycle 16 — Assigned entry → sets status Completed
    [Fact]
    public async Task Handle_AssignedEntry_SetsStatusCompleted()
    {
        var entry = WaitlistEntry.Create("user@example.com", Guid.NewGuid());
        entry.Assign(Guid.NewGuid(), Guid.NewGuid());

        _repoMock.Setup(r => r.GetByIdAsync(entry.Id, default)).ReturnsAsync(entry);
        _repoMock.Setup(r => r.UpdateAsync(entry, default)).Returns(Task.CompletedTask);

        await _handler.Handle(new CompleteAssignmentCommand(entry.Id), default);

        entry.Status.Should().Be(WaitlistEntry.StatusCompleted);
        _repoMock.Verify(r => r.UpdateAsync(entry, default), Times.Once);
    }

    // Cycle 16 — Null entry → does nothing
    [Fact]
    public async Task Handle_NullEntry_DoesNothing()
    {
        var entryId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(entryId, default)).ReturnsAsync((WaitlistEntry?)null);

        await _handler.Handle(new CompleteAssignmentCommand(entryId), default);

        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<WaitlistEntry>(), default), Times.Never);
    }
}
