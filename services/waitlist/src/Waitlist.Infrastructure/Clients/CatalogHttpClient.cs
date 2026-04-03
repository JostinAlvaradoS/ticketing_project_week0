using Waitlist.Application.Ports;

namespace Waitlist.Infrastructure.Clients;

public class CatalogHttpClient : ICatalogClient
{
    private readonly HttpClient _http;

    public CatalogHttpClient(HttpClient http) => _http = http;

    public async Task<int> GetAvailableCountAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"/api/v1/events/{eventId}/seats?status=available", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json  = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc   = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("count", out var count) ? count.GetInt32() : 0;
    }
}
