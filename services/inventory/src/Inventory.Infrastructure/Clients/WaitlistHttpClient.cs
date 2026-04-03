using System.Text.Json;
using Inventory.Application.Ports;

namespace Inventory.Infrastructure.Clients;

/// <summary>
/// HTTP adapter for the Waitlist Service port.
/// Calls GET /api/v1/waitlist/has-pending?eventId={eventId}
/// </summary>
public class WaitlistHttpClient : IWaitlistClient
{
    private readonly HttpClient _httpClient;

    public WaitlistHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<bool> HasPendingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient
            .GetAsync($"/api/v1/waitlist/has-pending?eventId={eventId:D}", cancellationToken)
            .ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("hasPending").GetBoolean();
    }
}
