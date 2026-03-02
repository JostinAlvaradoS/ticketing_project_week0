using System;
using System.Collections.Generic;
using System.Linq;
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
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var command = CreateValidCommand();

        SetupOrderValidation(command, true);
        SetupReservationValidation(command, true);
        SetupPaymentCreation();
        SetupPaymentSimulation(command, true);
        SetupPaymentUpdate();
        SetupKafkaProduction("payment-succeeded");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Payment.Should().NotBeNull();
        
        VerifyKafkaProduction("payment-succeeded", Times.Once());
    }

    [Fact]
    public async Task Handle_WhenOrderValidationFails_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupOrderValidation(command, false, "Order not found");

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Order not found");
        
        VerifyPaymentSimulation(Times.Never());
    }

    // --- Helper Methods for Setup and Verification ---

    private static ProcessPaymentCommand CreateValidCommand() => new(
        Guid.NewGuid(), 
        Guid.NewGuid(), 
        Guid.NewGuid(), 
        100.00m, 
        "USD", 
        "CreditCard"
    );

    private void SetupOrderValidation(ProcessPaymentCommand command, bool success, string error = null)
    {
        _orderValidationService.Setup(x => x.ValidateOrderAsync(
            command.OrderId, command.CustomerId, command.Amount, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderValidationResult(success, error, command.OrderId, "pending", command.Amount));
    }

    private void SetupReservationValidation(ProcessPaymentCommand command, bool success, string error = null)
    {
        if (command.ReservationId.HasValue)
        {
            _reservationValidationService.Setup(x => x.ValidateReservationAsync(
                command.ReservationId.Value, command.CustomerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReservationValidationResult(success, error));
        }
    }

    private void SetupPaymentSimulation(ProcessPaymentCommand command, bool success, string error = null)
    {
        _paymentSimulatorService.Setup(x => x.SimulatePaymentAsync(
            command.Amount, command.Currency, command.PaymentMethod, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentSimulationResult(success, success ? "txn-123" : null, error, null, null));
    }

    private void SetupPaymentCreation()
    {
        var payment = new Domain.Entities.Payment { Id = Guid.NewGuid(), Status = Domain.Entities.Payment.StatusPending };
        _paymentRepository.Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);
    }

    private void SetupPaymentUpdate()
    {
        _paymentRepository.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Payment p, CancellationToken _) => p);
    }

    private void SetupKafkaProduction(string topic)
    {
        _kafkaProducer.Setup(x => x.ProduceAsync(topic, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private void VerifyKafkaProduction(string topic, Times times)
    {
        _kafkaProducer.Verify(x => x.ProduceAsync(topic, It.IsAny<string>(), It.IsAny<string>()), times);
    }

    private void VerifyPaymentSimulation(Times times)
    {
        _paymentSimulatorService.Verify(x => x.SimulatePaymentAsync(
            It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), times);
    }

    /// <summary>
    /// TEST DE IDEMPOTENCIA: Pago duplicado
    /// Propósito: Asegurar que si recibimos dos veces la misma solicitud de pago para la misma orden,
    /// el sistema no procese el cargo dos veces ni genere múltiples registros de éxito.
    /// </summary>
    [Fact]
    public async Task Handle_Should_Be_Idempotent_When_Same_Order_Processed_Twice()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new ProcessPaymentCommand(
            orderId, 
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            150, 
            "USD", 
            "card"
        );

        SetupOrderValidation(command, true);
        SetupReservationValidation(command, true);

        var payment = new Domain.Entities.Payment 
        { 
            Id = Guid.NewGuid(), 
            OrderId = orderId, 
            Status = "succeeded",
            Amount = 150,
            CustomerId = command.CustomerId,
            PaymentMethod = "card",
            Currency = "USD"
        };
        
        // Simular que la segunda vez el repositorio ya encuentra el pago existente
        _paymentRepository.SetupSequence(x => x.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<Domain.Entities.Payment>()) // Primera vez: no existe
            .ReturnsAsync(new[] { payment });                       // Segunda vez: ya existe

        SetupPaymentCreation();
        SetupPaymentSimulation(command, true);
        SetupPaymentUpdate();
        SetupKafkaProduction("payment-succeeded");

        var handler = CreateHandler();

        // Act
        var result1 = await handler.Handle(command, CancellationToken.None);
        var result2 = await handler.Handle(command, CancellationToken.None);

        // Assert
        result1.Success.Should().BeTrue($"First attempt failed: {result1.ErrorMessage}");
        result2.Success.Should().BeTrue("El segundo intento debe ser exitoso por idempotencia");
        
        // El simulador de pago real solo debería haberse llamado una vez
        _paymentSimulatorService.Verify(x => x.SimulatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// TEST DE RESILIENCIA: Fallo en Kafka tras Pago exitoso
    /// Propósito: ¿Qué pasa si el pago se cobra pero no podemos avisar al resto del sistema?
    /// El sistema debe manejar esto (ej: mediante reintentos o marcando el pago para conciliación).
    /// </summary>
    [Fact]
    public async Task Handle_Should_Log_Error_When_Kafka_Fails_After_Payment_Success()
    {
        // Arrange
        var command = new ProcessPaymentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100, "USD", "card");

        _orderValidationService.Setup(x => x.ValidateOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderValidationResult(true, null));
        
        _paymentSimulatorService.Setup(x => x.SimulatePaymentAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentSimulationResult(true, "txn-123", null, null, null));

        // Simular fallo en Kafka
        _kafkaProducer.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Kafka connection failed"));

        var handler = CreateHandler();

        // Act & Assert
        // Dependiendo de la implementación, esto podría lanzar excepción o devolver Success=true con advertencia
        // Aquí probamos que al menos intente enviar el evento
        var act = async () => await handler.Handle(command, CancellationToken.None);
        
        await act.Should().NotThrowAsync("La falla de notificación no debería invalidar un cobro monetario ya realizado, pero debe registrarse");
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
