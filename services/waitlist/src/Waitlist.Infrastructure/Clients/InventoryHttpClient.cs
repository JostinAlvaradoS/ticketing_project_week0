using Waitlist.Application.Ports;

namespace Waitlist.Infrastructure.Clients;

public class InventoryHttpClient : IInventoryClient
{
    private readonly HttpClient _http;

    public InventoryHttpClient(HttpClient http)
    {
        _http = http;
    }

    public async Task ReleaseSeatAsync(Guid seatId, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync($"/seats/{seatId}/release", null, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }
}
