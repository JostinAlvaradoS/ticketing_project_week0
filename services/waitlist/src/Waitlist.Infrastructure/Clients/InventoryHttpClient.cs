using System.Text;
using Waitlist.Application.Ports;

namespace Waitlist.Infrastructure.Clients;

public class InventoryHttpClient : IInventoryClient
{
    private readonly HttpClient _http;

    public InventoryHttpClient(HttpClient http) => _http = http;

    public async Task ReleaseSeatAsync(Guid seatId, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsync(
            $"/api/v1/seats/{seatId}/release",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
