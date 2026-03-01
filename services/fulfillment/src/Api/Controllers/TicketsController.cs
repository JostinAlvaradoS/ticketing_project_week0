using Microsoft.AspNetCore.Mvc;
using Fulfillment.Application.Ports;
using Microsoft.AspNetCore.StaticFiles;

namespace Fulfillment.Api.Controllers;

[ApiController]
[Route("tickets")]
public class TicketsController : ControllerBase
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ILogger<TicketsController> _logger;
    private readonly string _storagePath;

    public TicketsController(ITicketRepository ticketRepository, ILogger<TicketsController> logger)
    {
        _ticketRepository = ticketRepository;
        _logger = logger;
        // Match the path used in LocalTicketStorageService
        _storagePath = Path.Combine(AppContext.BaseDirectory, "data", "tickets");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTicketPdf(string id)
    {
        // Try to parse as GUID for DB lookup
        if (id.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            id = id.Substring(0, id.Length - 4);
        }

        if (!Guid.TryParse(id, out var ticketId))
        {
            return BadRequest("Invalid ticket ID format. Expected a GUID.");
        }

        _logger.LogInformation("Requesting PDF for ticket {TicketId}", ticketId);
        
        var ticket = await _ticketRepository.GetByIdAsync(ticketId);
        if (ticket == null)
            return NotFound("Ticket not found in database");

        var fileName = $"{ticketId}.pdf";
        var filePath = Path.Combine(_storagePath, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("PDF file not found at path: {FilePath}", filePath);
            return NotFound("PDF file not found on disk");
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/pdf";
        }

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, contentType, fileName);
    }
}
