using System.Text;
using System.Text.Json;
using Waitlist.Application.Ports;

namespace Waitlist.Infrastructure.Clients;

public class OrderingHttpClient : IOrderingClient
{
    private readonly HttpClient _http;

    public OrderingHttpClient(HttpClient http) => _http = http;

    public async Task<Guid> CreateWaitlistOrderAsync(
        Guid seatId, decimal price, string guestToken, Guid concertEventId,
        CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            seatId         = seatId,
            price          = price,
            guestToken     = guestToken,
            concertEventId = concertEventId
        });

        var response = await _http.PostAsync(
            "/api/v1/orders/waitlist",
            new StringContent(body, Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json   = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc    = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("orderId").GetGuid();
    }

    public async Task CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var response = await _http.PatchAsync(
            $"/api/v1/orders/{orderId}/cancel",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
