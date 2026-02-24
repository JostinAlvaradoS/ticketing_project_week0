using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ordering.Application.DTOs;
using Ordering.Application.UseCases.AddToCart;

namespace Ordering.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CartController> _logger;

    public CartController(IMediator mediator, ILogger<CartController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Adds a reserved seat to the user's cart (creates draft order if none exists).
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.UserId) && string.IsNullOrEmpty(request.GuestToken))
        {
            return BadRequest("Either UserId or GuestToken must be provided");
        }

        var command = new AddToCartCommand(
            request.ReservationId,
            request.SeatId,
            request.Price,
            request.UserId,
            request.GuestToken
        );

        var response = await _mediator.Send(command, cancellationToken);

        if (!response.Success)
        {
            _logger.LogWarning("Failed to add seat {SeatId} to cart: {Error}", 
                request.SeatId, response.ErrorMessage);
            return BadRequest(response.ErrorMessage);
        }

        return Ok(response.Order);
    }
}