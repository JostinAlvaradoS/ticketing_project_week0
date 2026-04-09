using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.DTOs;
using Ordering.Application.Exceptions;
using Ordering.Application.UseCases.CancelOrder;
using Ordering.Application.UseCases.CheckoutOrder;
using Ordering.Application.UseCases.CreateWaitlistOrder;
using Ordering.Application.UseCases.GetOrder;

namespace Ordering.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Checks out an order, transitioning it from draft to pending state (ready for payment).
    /// </summary>
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.UserId) && string.IsNullOrEmpty(request.GuestToken))
        {
            return BadRequest("Either UserId or GuestToken must be provided");
        }

        var command = new CheckoutOrderCommand(
            request.OrderId,
            request.UserId,
            request.GuestToken
        );

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            _logger.LogWarning("Failed to checkout order {OrderId}: {Error}", 
                request.OrderId, response.ErrorMessage);
            
            return response.ErrorMessage switch
            {
                "Order not found" => NotFound(response.ErrorMessage),
                "Unauthorized" => Unauthorized(response.ErrorMessage),
                _ => BadRequest(response.ErrorMessage)
            };
        }

        return Ok(response.Order);
    }

    /// <summary>
    /// Creates an order directly in Pending state for a waitlist assignment.
    /// Called internally by the Waitlist Service. Uses guest token (email) as identity.
    /// Returns 409 if an active order for the same seat already exists.
    /// </summary>
    [HttpPost("waitlist")]
    public async Task<IActionResult> CreateWaitlistOrder([FromBody] CreateWaitlistOrderRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new CreateWaitlistOrderCommand(request.SeatId, request.GuestToken, request.Price);
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetOrderDetails), new { id = result.OrderId }, new { orderId = result.OrderId });
        }
        catch (DuplicateSeatOrderException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancels an order. Called by Waitlist Service during seat rotation.
    /// </summary>
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, CancellationToken cancellationToken = default)
    {
        var success = await _mediator.Send(new CancelOrderCommand(id), cancellationToken);
        if (!success)
            return NotFound(new { error = $"Order {id} not found or cannot be cancelled." });
        return NoContent();
    }

    /// <summary>
    /// Gets order details by ID.
    /// Used by Fulfillment service for ticket enrichment.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderDetails(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetOrderQuery(id), cancellationToken);
        
        if (result == null)
            return NotFound();

        var enrichment = new OrderEnrichmentDto(
            OrderId: result.Id,
            CustomerEmail: result.UserId, // null para guests — el consumidor decide el fallback
            EventId: result.EventId == Guid.Empty ? null : result.EventId,
            EventName: result.EventName == "Event Details Pending" ? null : result.EventName,
            SeatNumber: result.SeatNumber == "N/A" ? null : result.SeatNumber,
            Price: result.TotalAmount,
            Currency: "USD"
        );
        return Ok(enrichment);
    }
}