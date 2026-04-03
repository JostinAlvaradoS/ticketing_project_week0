using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Waitlist.Application.UseCases.CompleteAssignment;
using Waitlist.Infrastructure.Consumers;

namespace Waitlist.UnitTests.Application;

public class PaymentSucceededConsumerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly PaymentSucceededConsumer _consumer;

    public PaymentSucceededConsumerTests()
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock.Setup(x => x.GetService(typeof(IMediator))).Returns(_mediatorMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        var loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<PaymentSucceededConsumer>>();
        _consumer = new PaymentSucceededConsumer(scopeFactoryMock.Object, loggerMock.Object, "localhost:9092");
    }

    [Fact]
    public async Task ProcessMessage_WithWaitlistEntryId_DispatchesCompleteAssignmentCommand()
    {
        var entryId = Guid.NewGuid();
        var json = $"{{\"waitlistEntryId\":\"{entryId:D}\",\"orderId\":\"{Guid.NewGuid():D}\"}}";

        await _consumer.ProcessMessageAsync(json, default);

        _mediatorMock.Verify(m => m.Send(
            It.Is<CompleteAssignmentCommand>(c => c.EntryId == entryId),
            default), Times.Once);
    }

    [Fact]
    public async Task ProcessMessage_WithoutWaitlistEntryId_DoesNotDispatch()
    {
        var json = $"{{\"orderId\":\"{Guid.NewGuid():D}\"}}";

        await _consumer.ProcessMessageAsync(json, default);

        _mediatorMock.Verify(m => m.Send(It.IsAny<CompleteAssignmentCommand>(), default), Times.Never);
    }
}
