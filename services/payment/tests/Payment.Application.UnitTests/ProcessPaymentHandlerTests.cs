using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Events;
using Payment.Application.Ports;
using Payment.Application.UseCases.ProcessPayment;

namespace Payment.Application.UnitTests;

public class ProcessPaymentHandlerTests
{
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IOrderValidationService> _orderValidationService = new();
    private readonly Mock<IReservationValidationService> _reservationValidationService = new();
    private readonly Mock<IPaymentSimulatorService> _paymentSimulatorService = new();
    private readonly Mock<IKafkaProducer> _kafkaProducer = new();
    private readonly Mock<ILogger<ProcessPaymentHandler>> _logger = new();

    private ProcessPaymentHandler CreateHandler() => new(
        _paymentRepository.Object,
        _orderValidationService.Object,
        _reservationValidationService.Object,
        _paymentSimulatorService.Object,
        _kafkaProducer.Object,
        _logger.Object
    );

    [Fact]
    public async Task Handle_Should_Return_Success_When_Payment_Succeeds()
    {
        // Arrange
        var command = new ProcessPaymentCommand(
            Guid.NewGuid(), // OrderId
            Guid.NewGuid(), // CustomerId
            Guid.NewGuid(), // ReservationId
            100,           // Amount
            "USD",        // Currency
            "card"        // PaymentMethod
        );

        _orderValidationService.Setup(x => x.ValidateOrderAsync(
            command.OrderId, command.CustomerId, command.Amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderValidationResult(true, null, command.OrderId, "pending", command.Amount));

        _reservationValidationService.Setup(x => x.ValidateReservationAsync(
            command.ReservationId.Value, command.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReservationValidationResult(true, null));

        var payment = new Domain.Entities.Payment { Id = Guid.NewGuid(), Status = "pending" };
        _paymentRepository.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _paymentSimulatorService.Setup(x => x.SimulatePaymentAsync(
            command.Amount, command.Currency, command.PaymentMethod, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentSimulationResult(true, "txn-123", null, null, null));
        _paymentRepository.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Payment p, CancellationToken _) => p);
        _kafkaProducer.Setup(x => x.ProduceAsync(
            "payment-succeeded", It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Payment.Should().NotBeNull();
        _kafkaProducer.Verify(x => x.ProduceAsync("payment-succeeded", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Order_Validation_Fails()
    {
        // Arrange
        var command = new ProcessPaymentCommand(
            Guid.NewGuid(), // OrderId
            Guid.NewGuid(), // CustomerId
            null,           // ReservationId
            100,            // Amount
            "USD",         // Currency
            "card"         // PaymentMethod
        );

        _orderValidationService.Setup(x => x.ValidateOrderAsync(
            command.OrderId, command.CustomerId, command.Amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderValidationResult(false, "Order not found"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Order validation failed");
        result.Payment.Should().BeNull();
        _kafkaProducer.Verify(x => x.ProduceAsync("payment-succeeded", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Reservation_Validation_Fails()
    {
        // Arrange
        var command = new ProcessPaymentCommand(
            Guid.NewGuid(), // OrderId
            Guid.NewGuid(), // CustomerId
            Guid.NewGuid(), // ReservationId
            100,            // Amount
            "USD",         // Currency
            "card"         // PaymentMethod
        );

        _orderValidationService.Setup(x => x.ValidateOrderAsync(
            command.OrderId, command.CustomerId, command.Amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderValidationResult(true, null, command.OrderId, "pending", command.Amount));

        _reservationValidationService.Setup(x => x.ValidateReservationAsync(
            command.ReservationId.Value, command.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReservationValidationResult(false, "Reservation expired"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Reservation validation failed");
        result.Payment.Should().BeNull();
        _kafkaProducer.Verify(x => x.ProduceAsync("payment-succeeded", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_And_Publish_Event_When_Payment_Fails()
    {
        // Arrange
        var command = new ProcessPaymentCommand(
            Guid.NewGuid(), // OrderId
            Guid.NewGuid(), // CustomerId
            Guid.NewGuid(), // ReservationId
            100,            // Amount
            "USD",         // Currency
            "card"         // PaymentMethod
        );

        _orderValidationService.Setup(x => x.ValidateOrderAsync(
            command.OrderId, command.CustomerId, command.Amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderValidationResult(true, null, command.OrderId, "pending", command.Amount));

        _reservationValidationService.Setup(x => x.ValidateReservationAsync(
            command.ReservationId.Value, command.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReservationValidationResult(true, null));

        var payment = new Domain.Entities.Payment { Id = Guid.NewGuid(), Status = "pending" };
        _paymentRepository.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
        _paymentSimulatorService.Setup(x => x.SimulatePaymentAsync(
            command.Amount, command.Currency, command.PaymentMethod, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentSimulationResult(false, null, "ERR01", "Insufficient funds", "Declined"));
        _paymentRepository.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Payment p, CancellationToken _) => p);
        _kafkaProducer.Setup(x => x.ProduceAsync(
            "payment-failed", It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Payment failed");
        result.Payment.Should().NotBeNull();
        _kafkaProducer.Verify(x => x.ProduceAsync("payment-failed", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
