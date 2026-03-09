using Fulfillment.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Infrastructure.PdfGeneration;

public class LocalTicketStorageService : ITicketStorageService
{
    private readonly ILogger<LocalTicketStorageService> _logger;
    private readonly string _storagePath;

    public LocalTicketStorageService(ILogger<LocalTicketStorageService> logger)
    {
        _logger = logger;
        _storagePath = Path.Combine(AppContext.BaseDirectory, "data", "tickets");
        
        // Crear directorio si no existe
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> SaveTicketPdfAsync(Guid ticketId, Stream pdfStream)
    {
        try
        {
            var filename = $"{ticketId}.pdf";
            var filepath = Path.Combine(_storagePath, filename);

            using (var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                await pdfStream.CopyToAsync(fileStream);
            }

            // Retornar ruta relativa para almacenar en BD
            var relativePath = Path.Combine("tickets", filename).Replace("\\", "/");
            _logger.LogInformation($"Ticket PDF guardado en: {relativePath}");
            
            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error guardando PDF: {ex.Message}");
            throw;
        }
    }
}
