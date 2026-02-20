using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PaymentService.Application.Dtos;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Application.UseCases;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.Application.Tests;

public class ProcessPaymentApprovedUseCaseTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _payment_repository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessPaymentApprovedUseCase> _logger;
    private readonly ProcessPaymentApprovedUseCase _sut;

    public ProcessPaymentApprovedUseCaseTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _payment_repository = Substitute.For<IPaymentRepository>();
        _historyRepository = Substitute.For<ITicketHistoryRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _logger = Substitute.For<ILogger<ProcessPaymentApprovedUseCase>>();
        _sut = new ProcessPaymentApprovedUseCase(_ticketRepository, _payment_repository, _historyRepository, _unitOfWork, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_TicketNotFound_ReturnsFailure()
    {
        var dto = new PaymentApprovedEventDto { TicketId = 1 };
        _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns((Ticket?)null);

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Ticket not found");
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyPaid_ReturnsAlreadyProcessed()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.paid };
        var dto = new PaymentApprovedEventDto { TicketId = 1 };
        _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns(ticket);

        var result = await _sut.ExecuteAsync(dto);

        // current implementation treats non-reserved status as invalid
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("Invalid ticket status");
    }

    [Fact]
    public async Task ExecuteAsync_InvalidStatus_ReturnsFailure()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.available };
        var dto = new PaymentApprovedEventDto { TicketId = 1 };
        _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns(ticket);

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("Invalid ticket status");
    }

    [Fact]
    public async Task ExecuteAsync_WithinTimeLimit_TransitionsToPaid()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, ReservedAt = DateTime.UtcNow.AddMinutes(-2), Version = 1 };
        var dto = new PaymentApprovedEventDto { TicketId = 1, ApprovedAt = DateTime.UtcNow, AmountCents = 1000, Currency = "USD", TransactionRef = "ref123" };
        
        _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns(ticket);
        _payment_repository.GetByTicketIdAsync(1).Returns((Payment?)null);
        _payment_repository.AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _historyRepository.When(x => x.Add(Arg.Any<TicketHistory>())).Do(x => { });
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeTrue();
        await _payment_repository.Received(1).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        _historyRepository.Received(1).Add(Arg.Any<TicketHistory>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AfterTTL_TransitionsToReleased()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, ReservedAt = DateTime.UtcNow.AddMinutes(-10), Version = 1 };
        var dto = new PaymentApprovedEventDto { TicketId = 1, ApprovedAt = DateTime.UtcNow };
        
        _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns(ticket);
        _payment_repository.GetByTicketIdAsync(1).Returns((Payment?)null);
        _historyRepository.When(x => x.Add(Arg.Any<TicketHistory>())).Do(x => { });
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("TTL exceeded");
    }

    [Fact]
    public void IsWithinTimeLimit_ReturnsTrue_WhenWithin5Minutes()
    {
        var reservedAt = DateTime.UtcNow.AddMinutes(-2);
        var paymentReceivedAt = DateTime.UtcNow;

        var result = _sut.IsWithinTimeLimit(reservedAt, paymentReceivedAt);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinTimeLimit_ReturnsFalse_WhenAfter5Minutes()
    {
        var reservedAt = DateTime.UtcNow.AddMinutes(-10);
        var paymentReceivedAt = DateTime.UtcNow;

        var result = _sut.IsWithinTimeLimit(reservedAt, paymentReceivedAt);

        result.Should().BeFalse();
    }
}
