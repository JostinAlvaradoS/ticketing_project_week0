using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.DTOs;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence;
using Xunit;

namespace Ordering.IntegrationTests;

public class OrdersIntegrationTests : IClassFixture<OrderingApiFactory>
{
    private readonly HttpClient _client;
    private readonly OrderingApiFactory _factory;

    public OrdersIntegrationTests(OrderingApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetOrderDetails_WhenOrderExists_ReturnsSuccess()
    {
        // Arrange: Seed an order in the In-Memory DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        
        var testOrder = Order.Create("test-user-123", null);
        var orderId = testOrder.Id;
        db.Orders.Add(testOrder);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/Orders/{orderId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        // Check partial properties since it's an anonymous object in the controller
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrderDetails_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"/Orders/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
