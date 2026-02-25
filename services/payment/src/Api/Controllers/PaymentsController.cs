using MediatR;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.DTOs;
using Payment.Application.UseCases.ProcessPayment;

namespace Payment.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMediator mediator, ILogger<PaymentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Processes a payment for an order.
    /// Validates order state, checks reservation, simulates payment, and publishes events.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return BadRequest("Payment request is required");
        }

        if (request.OrderId == Guid.Empty)
        {
            return BadRequest("Valid OrderId is required");
        }

        if (request.CustomerId == Guid.Empty)
        {
            return BadRequest("Valid CustomerId is required");
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than zero");
        }

        _logger.LogInformation("Processing payment request for order {OrderId}, customer {CustomerId}, amount {Amount}",
            request.OrderId, request.CustomerId, request.Amount);

        var command = new ProcessPaymentCommand(
            request.OrderId,
            request.CustomerId,
            request.ReservationId,
            request.Amount,
            request.Currency,
            request.PaymentMethod
        );

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            _logger.LogWarning("Payment processing failed for order {OrderId}: {Error}", 
                request.OrderId, response.ErrorMessage);

            // Determine appropriate HTTP status based on error message
            return response.ErrorMessage switch
            {
                var msg when msg?.Contains("Order not found") == true => NotFound(response),
                var msg when msg?.Contains("Order validation failed") == true => BadRequest(response),
                var msg when msg?.Contains("Reservation validation failed") == true => BadRequest(response),
                var msg when msg?.Contains("Unauthorized") == true => Unauthorized(response),
                var msg when msg?.Contains("Payment failed") == true => UnprocessableEntity(response),
                _ => StatusCode(500, response)
            };
        }

        _logger.LogInformation("Payment processing succeeded for order {OrderId}, payment {PaymentId}", 
            request.OrderId, response.Payment?.Id);

        return Ok(response);
    }
}