// TDD Ciclos 7-11 — Spec US1: Registro en Lista de Espera
// STATUS: 🔴 RED — JoinWaitlistHandler, ports, exceptions do not exist yet

using FluentAssertions;
using Moq;
using Waitlist.Application.Exceptions;
using Waitlist.Application.Ports;
using Waitlist.Application.UseCases.JoinWaitlist;
using Waitlist.Domain.Entities;

namespace Waitlist.UnitTests.Application;

public class JoinWaitlistHandlerTests
{
    private readonly Mock<IWaitlistRepository> _repoMock;
    private readonly Mock<ICatalogClient>      _catalogMock;
    private readonly JoinWaitlistHandler       _handler;

    public JoinWaitlistHandlerTests()
    {
        _repoMock    = new Mock<IWaitlistRepository>();
        _catalogMock = new Mock<ICatalogClient>();
        _handler     = new JoinWaitlistHandler(_repoMock.Object, _catalogMock.Object);
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 7 — Spec US1 Scenario 1
    // Given stock=0, email válido → 201 con posición en cola
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidEmail_StockZero_CreatesEntryAndReturnsPosition()
    {
        // Arrange
        var command = new JoinWaitlistCommand("jostin@example.com", Guid.NewGuid());

        _catalogMock
            .Setup(x => x.GetAvailableCountAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _repoMock
            .Setup(x => x.HasActiveEntryAsync(command.Email, command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repoMock
            .Setup(x => x.GetQueuePositionAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Position.Should().Be(1);
        result.EntryId.Should().NotBe(Guid.Empty);
        _repoMock.Verify(x => x.AddAsync(It.Is<WaitlistEntry>(e =>
            e.Email == command.Email &&
            e.EventId == command.EventId &&
            e.Status == WaitlistEntry.StatusPending), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 8 — Spec US1 Scenario 2
    // Given stock > 0 → 409 Conflict
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_StockAvailable_ThrowsWaitlistConflictException()
    {
        // Arrange
        var command = new JoinWaitlistCommand("jostin@example.com", Guid.NewGuid());

        _catalogMock
            .Setup(x => x.GetAvailableCountAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WaitlistConflictException>()
            .WithMessage("*disponibles*");
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 9 — Spec US1 Scenario 3
    // Given email duplicado activo (Pending o Assigned) → 409
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateActiveEntry_ThrowsWaitlistConflictException()
    {
        // Arrange
        var command = new JoinWaitlistCommand("jostin@example.com", Guid.NewGuid());

        _catalogMock
            .Setup(x => x.GetAvailableCountAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _repoMock
            .Setup(x => x.HasActiveEntryAsync(command.Email, command.EventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WaitlistConflictException>()
            .WithMessage("*lista*");
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 11 — Edge case: Catalog Service no disponible → 503
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_CatalogClientThrows_ThrowsServiceUnavailableException()
    {
        // Arrange
        var command = new JoinWaitlistCommand("jostin@example.com", Guid.NewGuid());

        _catalogMock
            .Setup(x => x.GetAvailableCountAsync(command.EventId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connection refused"));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<WaitlistServiceUnavailableException>();
    }

    // ─────────────────────────────────────────────────────────────
    // Ciclo 10 — Spec US1 Scenario 4: email inválido
    // El validator de FluentValidation se testea por separado;
    // el handler recibe el comando ya validado por el pipeline.
    // Verificamos que el validator rechaza emails malformados.
    // ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@nodomain")]
    [InlineData("no-at-sign")]
    public void JoinWaitlistCommandValidator_InvalidEmail_HasValidationError(string invalidEmail)
    {
        // Arrange
        var validator = new JoinWaitlistCommandValidator();
        var command   = new JoinWaitlistCommand(invalidEmail, Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Email));
    }

    [Fact]
    public void JoinWaitlistCommandValidator_ValidEmail_PassesValidation()
    {
        // Arrange
        var validator = new JoinWaitlistCommandValidator();
        var command   = new JoinWaitlistCommand("valid@example.com", Guid.NewGuid());

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
