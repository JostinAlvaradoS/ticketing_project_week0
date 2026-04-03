using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Waitlist.Application.Ports;

namespace Waitlist.Infrastructure.Clients;

public class OrderingHttpClient : IOrderingClient
{
    private readonly HttpClient _http;

    public OrderingHttpClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<Guid> CreateWaitlistOrderAsync(
        string email, Guid seatId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var payload = new { email, seatId, eventId };
        var response = await _http.PostAsJsonAsync("/api/v1/orders/waitlist", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("orderId", out var orderIdProp) &&
            Guid.TryParse(orderIdProp.GetString(), out var orderId))
            return orderId;

        throw new InvalidOperationException("Ordering service did not return a valid orderId.");
    }

    public async Task CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/v1/orders/{orderId}", cancellationToken);
        // Ignore 404 — idempotent cancel
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }
}
