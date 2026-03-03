using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Fulfillment.Application.Ports;
using Fulfillment.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Infrastructure.PdfGeneration;

public class TicketPdfGenerator : ITicketPdfGenerator
{
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<TicketPdfGenerator> _logger;

    public TicketPdfGenerator(IQrCodeService qrCodeService, ILogger<TicketPdfGenerator> logger)
    {
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    public async Task<(Stream pdfStream, string filename)> GenerateTicketPdfAsync(Ticket ticket)
    {
        try
        {
            _logger.LogInformation($"Generando PDF para ticket {ticket.Id}");

            // Crear documento PDF
            var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            // Definir fuentes
            var titleFont = new XFont("Courier", 28, XFontStyle.Bold);
            var labelFont = new XFont("Courier", 11, XFontStyle.Bold);
            var valueFont = new XFont("Courier", 10, XFontStyle.Regular);
            var smallFont = new XFont("Courier", 8, XFontStyle.Italic);

            // Colores
            var darkColor = XBrushes.DarkBlue;
            var lightColor = XBrushes.Gray;

            // Márgenes (en puntos, 72 puntos = 1 inch)
            double margin = 40;
            double currentY = margin;
            double lineHeight = 16;

            // === ENCABEZADO ===
            currentY += 20;
            gfx.DrawString("TICKET DE ENTRADA", titleFont, darkColor, 
                new XRect(margin, currentY, page.Width - 2 * margin, 40), 
                XStringFormats.TopCenter);
            currentY += 50;

            // Línea separadora
            gfx.DrawLine(XPens.Black, margin, currentY, page.Width - margin, currentY);
            currentY += 20;

            // === INFORMACIÓN DEL EVENTO ===
            DrawField(gfx, "EVENTO", labelFont, margin, ref currentY, lineHeight);
            DrawField(gfx, "Nombre: " + ticket.EventName, valueFont, margin + 20, ref currentY, lineHeight);
            DrawField(gfx, "Asiento: " + ticket.SeatNumber, valueFont, margin + 20, ref currentY, lineHeight);
            DrawField(gfx, "Precio: " + ticket.Price + " " + ticket.Currency, valueFont, margin + 20, ref currentY, lineHeight);

            currentY += 15;

            // === INFORMACIÓN DEL CLIENTE ===
            DrawField(gfx, "CLIENTE", labelFont, margin, ref currentY, lineHeight);
            DrawField(gfx, "Email: " + ticket.CustomerEmail, valueFont, margin + 20, ref currentY, lineHeight);
            DrawField(gfx, "Generado: " + ticket.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"), valueFont, margin + 20, ref currentY, lineHeight);

            currentY += 15;

            // === CÓDIGO QR ===
            DrawField(gfx, "CÓDIGO QR", labelFont, margin, ref currentY, lineHeight);
            DrawField(gfx, ticket.QrCodeData, valueFont, margin + 20, ref currentY, lineHeight);

            currentY = page.Height - margin - 30;
            gfx.DrawLine(XPens.Black, margin, currentY, page.Width - margin, currentY);
            currentY += 10;

            gfx.DrawString("Este ticket es válido solo con identificación oficial", 
                smallFont, 
                lightColor,
                new XRect(margin, currentY, page.Width - 2 * margin, 20),
                XStringFormats.BottomCenter);

            // Guardar a stream
            var pdfStream = new MemoryStream();
            document.Save(pdfStream, false);
            pdfStream.Position = 0;

            document.Close();

            return (pdfStream, $"{ticket.Id}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error generando PDF: {ex.Message}");
            throw;
        }
    }

    private void DrawField(XGraphics gfx, string text, XFont font, double x, ref double y, double lineHeight)
    {
        gfx.DrawString(text, font, XBrushes.Black, x, y);
        y += lineHeight;
    }
}
