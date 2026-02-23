using Microsoft.AspNetCore.Mvc;

namespace Ordering.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Status = "Healthy", Service = "Ordering" });
    }
}