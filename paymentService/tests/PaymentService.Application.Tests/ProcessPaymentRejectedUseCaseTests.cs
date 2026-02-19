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

public class ProcessPaymentRejectedUseCaseTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<ProcessPaymentRejectedUseCase> _logger;
    private readonly ProcessPaymentRejectedUseCase _sut;

    public ProcessPaymentRejectedUseCaseTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _paymentRepository = Substitute.For<IPaymentRepository>();
        _historyRepository = Substitute.For<ITicketHistoryRepository>();
        _logger = Substitute.For<ILogger<ProcessPaymentRejectedUseCase>>();
        _sut = new ProcessPaymentRejectedUseCase(_ticketRepository, _paymentRepository, _historyRepository, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_TicketNotFound_ReturnsFailure()
    {
        var dto = new PaymentRejectedEventDto { TicketId = 1 };
        _ticketRepository.GetByIdAsync(1).Returns((Ticket?)null);

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Ticket not found");
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyReleased_ReturnsAlreadyProcessed()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.released };
        var dto = new PaymentRejectedEventDto { TicketId = 1 };
        _ticketRepository.GetByIdAsync(1).Returns(ticket);

        var result = await _sut.ExecuteAsync(dto);

        result.IsAlreadyProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_TransitionsToReleased()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, Version = 1 };
        var dto = new PaymentRejectedEventDto { TicketId = 1, RejectionReason = "Card declined" };
        
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _ticketRepository.GetByIdForUpdateAsync(1).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(true);
        _historyRepository.AddAsync(Arg.Any<TicketHistory>()).Returns(Task.CompletedTask);

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeTrue();
        await _historyRepository.Received(1).AddAsync(Arg.Any<TicketHistory>());
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingPayment_UpdatesPaymentStatus()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, Version = 1 };
        var payment = new Payment { Id = 1, TicketId = 1, Status = PaymentStatus.pending };
        var dto = new PaymentRejectedEventDto { TicketId = 1, RejectionReason = "Card declined" };
        
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _ticketRepository.GetByIdForUpdateAsync(1).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(true);
        _paymentRepository.GetByTicketIdAsync(1).Returns(payment);
        _paymentRepository.UpdateAsync(Arg.Any<Payment>()).Returns(true);
        _historyRepository.AddAsync(Arg.Any<TicketHistory>()).Returns(Task.CompletedTask);

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeTrue();
        await _paymentRepository.Received(1).UpdateAsync(Arg.Any<Payment>());
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrencyIssue_ReturnsFailure()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, Version = 1 };
        var dto = new PaymentRejectedEventDto { TicketId = 1, RejectionReason = "Card declined" };
        
        _ticketRepository.GetByIdAsync(1).Returns(ticket);
        _ticketRepository.GetByIdForUpdateAsync(1).Returns(ticket);
        _ticketRepository.UpdateAsync(Arg.Any<Ticket>()).Returns(false);
        _ticketRepository.GetByIdAsync(1).Returns(new Ticket { Id = 1, Status = TicketStatus.available });

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be("Failed to transition ticket to released status");
    }
}
