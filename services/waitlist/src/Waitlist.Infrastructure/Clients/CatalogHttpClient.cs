using System.Text.Json;
using Waitlist.Application.Ports;

namespace Waitlist.Infrastructure.Clients;

public class CatalogHttpClient : ICatalogClient
{
    private readonly HttpClient _http;

    public CatalogHttpClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<int> GetAvailableCountAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"/events/{eventId}/seatmap", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return 0;

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("seats", out var seats))
            return 0;

        int count = 0;
        foreach (var seat in seats.EnumerateArray())
            if (seat.TryGetProperty("status", out var status) && status.GetString() == "available")
                count++;

        return count;
    }
}
