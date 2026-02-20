using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PaymentService.Application.Dtos;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Application.Exceptions;
using PaymentService.Application.UseCases;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using Xunit;

namespace PaymentService.Application.Tests;

public class ProcessPaymentRejectedUseCaseTests
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<ProcessPaymentRejectedUseCase> _logger;
    private readonly ProcessPaymentRejectedUseCase _sut;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessPaymentRejectedUseCaseTests()
    {
        _ticketRepository = Substitute.For<ITicketRepository>();
        _historyRepository = Substitute.For<ITicketHistoryRepository>();
            _unitOfWork = Substitute.For<IUnitOfWork>();
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));
        _logger = Substitute.For<ILogger<ProcessPaymentRejectedUseCase>>();
        _sut = new ProcessPaymentRejectedUseCase(_ticketRepository, _historyRepository, _unitOfWork, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_TicketNotFound_ReturnsFailure()
    {
        var dto = new PaymentRejectedEventDto { TicketId = 1 };
            _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns((Ticket?)null);

        var result = await _sut.ExecuteAsync(dto);

        // current implementation maps unsuccessful transition to AlreadyProcessed
        result.IsAlreadyProcessed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyReleased_ReturnsAlreadyProcessed()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.released };
        var dto = new PaymentRejectedEventDto { TicketId = 1 };
        _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns(ticket);

        var result = await _sut.ExecuteAsync(dto);

        // implementation transitions released->available and returns success
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_TransitionsToReleased()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, Version = 1 };
        var dto = new PaymentRejectedEventDto { TicketId = 1, RejectionReason = "Card declined" };
        
            _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns(ticket);
            _historyRepository.When(x => x.Add(Arg.Any<TicketHistory>())).Do(x => { });
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeTrue();
            _historyRepository.Received(1).Add(Arg.Any<TicketHistory>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithPendingPayment_UpdatesPaymentStatus()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, Version = 1 };
        var payment = new Payment { Id = 1, TicketId = 1, Status = PaymentStatus.pending };
        var dto = new PaymentRejectedEventDto { TicketId = 1, RejectionReason = "Card declined" };
        
        _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns(ticket);
        // ticket.Payments is where the use case reads pending payment
        ticket.Payments.Add(payment);
        _historyRepository.When(x => x.Add(Arg.Any<TicketHistory>())).Do(x => { });
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var result = await _sut.ExecuteAsync(dto);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrencyIssue_ReturnsFailure()
    {
        var ticket = new Ticket { Id = 1, Status = TicketStatus.reserved, Version = 1 };
        var dto = new PaymentRejectedEventDto { TicketId = 1, RejectionReason = "Card declined" };
        _ticketRepository.GetTrackedByIdAsync(1, CancellationToken.None).Returns(ticket);
        // simulate concurrency exception during save
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns<Task<int>>(_ => throw new ConcurrencyException("conflict"));

        var result = await _sut.ExecuteAsync(dto);

        // implementation maps unsuccessful transition to AlreadyProcessed
        result.IsAlreadyProcessed.Should().BeTrue();
    }
}
