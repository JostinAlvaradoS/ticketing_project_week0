using QRCoder;
using System.Drawing;
using Fulfillment.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Infrastructure.PdfGeneration;

public class QrCodeService : IQrCodeService
{
    private readonly ILogger<QrCodeService> _logger;

    public QrCodeService(ILogger<QrCodeService> logger)
    {
        _logger = logger;
    }

    public async Task<Stream> GenerateQrCodeAsync(string data)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation($"Generando QR code para: {data}");

                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
                    
                    // Usar el generador de imagenes basado en caracteres
                    using (var qrCode = new PngByteQRCode(qrCodeData))
                    {
                        var qrImage = qrCode.GetGraphic(10);
                        var stream = new MemoryStream(qrImage);
                        stream.Position = 0;
                        return stream;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generando QR code: {ex.Message}");
                // Retornar stream vacío en lugar de fallar
                return new MemoryStream();
            }
        });
    }
}
