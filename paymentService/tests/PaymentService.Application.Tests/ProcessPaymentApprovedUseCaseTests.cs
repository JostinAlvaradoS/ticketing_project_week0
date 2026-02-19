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
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<ProcessPaymentApprovedUseCase> _logger;
    private readonly ProcessPaymentApprovedUseCase _sut;

    public ProcessPaymentApprovedUseCaseTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _paymentRepository = Substitute.For<IPaymentRepository>();
        _historyRepository = Substitute.For<ITicketHistoryRepository>();
        _logger = Substitute.For<ILogger<ProcessPaymentApprovedUseCase>>();
        _sut = new ProcessPaymentApprovedUseCase(_ticketRepository, _paymentRepository, _historyRepository, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_TicketNotFound_ReturnsFailure()
    {
        var dto = new PaymentApprovedEventDto { TicketId = 1 };
        _ticketRepository.GetByIdAsync(1).Returns((Ticket?)null);

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Ticket not found");
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyPaid_ReturnsAlreadyProcessed()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.paid };
        var dto = new PaymentApprovedEventDto { TicketId = 1 };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);

        var result = await _sut.ExecuteAsync(dto);

        result.IsAlreadyProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_InvalidStatus_ReturnsFailure()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.available };
        var dto = new PaymentApprovedEventDto { TicketId = 1 };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Contain("Invalid ticket status");
    }

    [Fact]
    public async Task ExecuteAsync_WithinTimeLimit_TransitionsToPaid()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, ReservedAt = DateTime.UtcNow.AddMinutes(-2), Version = 1 };
        var dto = new PaymentApprovedEventDto { TicketId = 1, ApprovedAt = DateTime.UtcNow, AmountCents = 1000, Currency = "USD", TransactionRef = "ref123" };
        
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _paymentRepository.GetByTicketIdAsync(1).Returns((Payment?)null);
        _ticketRepository.GetByIdForUpdateAsync(1).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(true);
        _paymentRepository.CreateAsync(Arg.Any<Payment>()).Returns(new Payment { Id = 1, TicketId = 1 });
        _historyRepository.AddAsync(Arg.Any<TicketHistory>()).Returns(Task.CompletedTask);

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeTrue();
        await _paymentRepository.Received(1).CreateAsync(Arg.Any<Payment>());
        await _historyRepository.Received(1).AddAsync(Arg.Any<TicketHistory>());
    }

    [Fact]
    public async Task ExecuteAsync_AfterTTL_TransitionsToReleased()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, ReservedAt = DateTime.UtcNow.AddMinutes(-10), Version = 1 };
        var dto = new PaymentApprovedEventDto { TicketId = 1, ApprovedAt = DateTime.UtcNow };
        
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _paymentRepository.GetByTicketIdAsync(1).Returns((Payment?)null);
        _ticketRepository.GetByIdForUpdateAsync(1).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(true);
        _historyRepository.AddAsync(Arg.Any<TicketHistory>()).Returns(Task.CompletedTask);

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
