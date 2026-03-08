using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using Xunit;

namespace Catalog.IntegrationTests;

public class CatalogIntegrationTests : IClassFixture<CatalogApiFactory>
{
    private readonly HttpClient _client;
    private readonly CatalogApiFactory _factory;

    public CatalogIntegrationTests(CatalogApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEvents_ReturnsSuccess()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        
        var testEvent = Event.Create(
            "Test Event", 
            "Test Desc",
            DateTime.UtcNow.AddDays(10),
            "Test Venue",
            100,
            50.00m
        );
        db.Events.Add(testEvent);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/events");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
