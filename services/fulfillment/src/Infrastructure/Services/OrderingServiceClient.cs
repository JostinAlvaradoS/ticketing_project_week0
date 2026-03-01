using System.Net.Http.Json;
using Fulfillment.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Infrastructure.Services;

public class OrderingServiceClient : IOrderingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderingServiceClient> _logger;

    public OrderingServiceClient(HttpClient httpClient, ILogger<OrderingServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OrderDetailsDto?> GetOrderDetailsAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching order details for {OrderId} from Ordering service", orderId);
            
            var response = await _httpClient.GetAsync($"/Orders/{orderId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch order details for {OrderId}. Status: {StatusCode}", orderId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<OrderDetailsDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Ordering service for order {OrderId}", orderId);
            return null;
        }
    }
}
