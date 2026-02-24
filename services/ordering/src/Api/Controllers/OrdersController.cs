using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.DTOs;
using Ordering.Application.UseCases.CheckoutOrder;

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
}