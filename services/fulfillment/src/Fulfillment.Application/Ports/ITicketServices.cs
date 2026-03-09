using Fulfillment.Domain.Entities;

namespace Fulfillment.Application.Ports;

public interface ITicketPdfGenerator
{
    /// <summary>
    /// Genera un PDF de ticket con código QR incluido
    /// </summary>
    /// <param name="ticket">Entidad de ticket con datos del evento</param>
    /// <returns>Stream del PDF generado</returns>
    Task<(Stream pdfStream, string filename)> GenerateTicketPdfAsync(Ticket ticket);
}

public interface IQrCodeService
{
    /// <summary>
    /// Genera un código QR en formato de imagen
    /// </summary>
    /// <param name="data">Datos a codificar en el QR (ej: order_id:seat:event_id)</param>
    /// <returns>Stream de imagen PNG con el QR code</returns>
    Task<Stream> GenerateQrCodeAsync(string data);
}

public interface ITicketStorageService
{
    /// <summary>
    /// Almacena un archivo PDF de ticket
    /// </summary>
    /// <param name="ticketId">ID del ticket</param>
    /// <param name="pdfStream">Stream del PDF a guardar</param>
    /// <returns>Ruta relativa del archivo guardado</returns>
    Task<string> SaveTicketPdfAsync(Guid ticketId, Stream pdfStream);
}
