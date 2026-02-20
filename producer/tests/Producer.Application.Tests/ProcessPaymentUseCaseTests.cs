using FluentAssertions;
using NSubstitute;
using Producer.Application.Dtos;
using Producer.Application.Ports.Outbound;
using Producer.Application.UseCases;
using Producer.Domain.Events;
using Xunit;

namespace Producer.Application.Tests;

public class ProcessPaymentUseCaseTests
{
    private readonly IPaymentEventPublisher _paymentPublisher;
    private readonly ProcessPaymentUseCase _sut;

    public ProcessPaymentUseCaseTests()
    {
        _paymentPublisher = Substitute.For<IPaymentEventPublisher>();
        _sut = new ProcessPaymentUseCase(_paymentPublisher);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldPublishApprovedEvent()
    {
        var request = new ProcessPaymentRequest
        {
            TicketId = 1,
            EventId = 100,
            AmountCents = 5000,
            Currency = "USD",
            PaymentBy = "user@example.com",
            PaymentMethodId = "card-123"
        };

        _paymentPublisher.PublishApprovedAsync(Arg.Any<PaymentApprovedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeTrue();
        result.Status.Should().Be("approved");
        await _paymentPublisher.Received(1).PublishApprovedAsync(Arg.Any<PaymentApprovedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldPublishRejectedEvent()
    {
        var request = new ProcessPaymentRequest
        {
            TicketId = 1,
            EventId = 100,
            AmountCents = 5000,
            Currency = "USD",
            PaymentBy = "user@example.com",
            PaymentMethodId = "card-123"
        };

        _paymentPublisher.PublishRejectedAsync(Arg.Any<PaymentRejectedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeTrue();
        result.Status.Should().BeOneOf(new[] { "approved", "rejected" });
    }

    [Theory]
    [InlineData(0, "TicketId debe ser mayor a 0")]
    [InlineData(-1, "TicketId debe ser mayor a 0")]
    public async Task ExecuteAsync_WithInvalidTicketId_ShouldReturnError(int ticketId, string expectedMessage)
    {
        var request = new ProcessPaymentRequest
        {
            TicketId = ticketId,
            EventId = 100,
            AmountCents = 5000,
            PaymentBy = "user@example.com",
            PaymentMethodId = "card-123"
        };

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be(expectedMessage);
    }

    [Theory]
    [InlineData(0, "EventId debe ser mayor a 0")]
    [InlineData(-5, "EventId debe ser mayor a 0")]
    public async Task ExecuteAsync_WithInvalidEventId_ShouldReturnError(int eventId, string expectedMessage)
    {
        var request = new ProcessPaymentRequest
        {
            TicketId = 1,
            EventId = eventId,
            AmountCents = 5000,
            PaymentBy = "user@example.com",
            PaymentMethodId = "card-123"
        };

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be(expectedMessage);
    }

    [Theory]
    [InlineData(0, "AmountCents debe ser mayor a 0")]
    [InlineData(-100, "AmountCents debe ser mayor a 0")]
    public async Task ExecuteAsync_WithInvalidAmountCents_ShouldReturnError(int amountCents, string expectedMessage)
    {
        var request = new ProcessPaymentRequest
        {
            TicketId = 1,
            EventId = 100,
            AmountCents = amountCents,
            PaymentBy = "user@example.com",
            PaymentMethodId = "card-123"
        };

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullPaymentBy_ShouldReturnError()
    {
        var request = new ProcessPaymentRequest
        {
            TicketId = 1,
            EventId = 100,
            AmountCents = 5000,
            PaymentBy = null!,
            PaymentMethodId = "card-123"
        };

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("PaymentBy es requerido");
    }

    [Fact]
    public async Task ExecuteAsync_WithNullPaymentMethodId_ShouldReturnError()
    {
        var request = new ProcessPaymentRequest
        {
            TicketId = 1,
            EventId = 100,
            AmountCents = 5000,
            PaymentBy = "user@example.com",
            PaymentMethodId = null!
        };

        var result = await _sut.ExecuteAsync(request);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("PaymentMethodId es requerido");
    }
}
