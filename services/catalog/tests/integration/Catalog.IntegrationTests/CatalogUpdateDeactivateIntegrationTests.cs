using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Reflection;
using Microsoft.IdentityModel.Tokens;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Testcontainers.PostgreSql;
using Catalog.Infrastructure.Persistence;
using Catalog.Application.UseCases.GetEventSeatmap;
using Catalog.Domain.Entities;
using Xunit;

namespace Catalog.IntegrationTests;

/// <summary>
/// T106 Integration Test: Escenarios de Actualización y Desactivación (Soft Delete) de eventos
/// 
/// This test validates the T106 scenarios as defined in catalog-admin.feature:
/// 
/// Escenario: Actualizar detalles de un evento existente (T106-A)
///   Dado que tengo un evento "Tech Conference 2025" ya creado
///   Cuando actualizo el evento con:
///     | name | Tech Conference 2025 Advanced |
///     | description | The most advanced tech conference |
///     | max_capacity | 1500 |
///   Entonces la operación es exitosa
///   Y el evento refleja los nuevos datos
/// 
/// Escenario: Desactivar evento sin reservas activas (T106-B)
///   Dado que tengo un evento "Music Festival" sin reservas activas
///   Cuando desactivo el evento
///   Entonces la operación es exitosa
///   Y el evento tiene status "inactive"
///   Y todos los asientos tienen status "unavailable"
/// 
/// Escenario: Reactivar evento con fecha futura válida (T106-C)
///   Dado que tengo un evento desactivado con fecha futura
///   Cuando reactivo el evento
///   Entonces la operación es exitosa
///   Y el evento tiene status "active"
///   Y todos los asientos tienen status "available"
/// 
/// Escenario: Intentar desactivar evento con reservas activas (T106-D)
///   Dado que tengo un evento "Popular Concert" con reservas activas
///   Cuando intento desactivar el evento
///   Entonces recibo error 400 BadRequest
///   Y el mensaje indica "No se puede desactivar un evento con reservas activas"
/// 
/// Escenario: Intentar reactivar evento con fecha pasada (T106-E)
///   Dado que tengo un evento desactivado con fecha en el pasado
///   Cuando intento reactivar el evento
///   Entonces recibo error 400 BadRequest
///   Y el mensaje indica "No se puede reactivar un evento que ya pasó"
///
/// DEPENDENCIES:
/// - T100-T105: ✅ All previous catalog functionality (COMPLETED)
/// - T106: ✅ Update/Deactivate/Reactivate domain logic (COMPLETED)
/// - T106: ✅ Command handlers and API endpoints (COMPLETED)
/// </summary>
[Trait("Category", "IntegrationTest")]
[Trait("Task", "T106")]
public class T106_CatalogUpdateDeactivateIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresql = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private HttpClient _adminClient = null!;
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        // Start PostgreSQL container
        _postgresql = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("catalog_t106_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgresql.StartAsync();
        _connectionString = _postgresql.GetConnectionString();

        // Create web application factory
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = _connectionString,
                        ["Jwt:Key"] = "dev-secret-key-minimum-32-chars-required-for-security",
                        ["Jwt:Issuer"] = "SpecKit.Identity",
                        ["Jwt:Audience"] = "SpecKit.Services"
                    });
                });
                
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<CatalogDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    
                    // Add test database context
                    services.AddDbContext<CatalogDbContext>(options =>
                    {
                        options.UseNpgsql(_connectionString, opt =>
                        {
                            opt.MigrationsHistoryTable("__EFMigrationsHistory", "bc_catalog");
                        });
                    });
                });
            });

        _client = _factory.CreateClient();
        
        // Create authenticated admin client
        _adminClient = _factory.CreateClient();
        var adminToken = GenerateAdminToken();
        _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Initialize database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        _adminClient?.Dispose();
        
        if (_factory != null)
            await _factory.DisposeAsync();
            
        if (_postgresql != null)
        {
            await _postgresql.StopAsync();
            await _postgresql.DisposeAsync();
        }
    }

    [Fact]
    public async Task T106A_UpdateEventDetails_UpdatesSuccessfully()
    {
        // ARRANGE: Given I have an event "Tech Conference 2025" already created
        var eventId = await CreateTestEvent("Tech Conference 2025", "Initial tech conference");

        var updateRequest = new
        {
            Name = "Tech Conference 2025 Advanced",
            Description = "The most advanced tech conference", 
            MaxCapacity = 1500,
            BasePrice = 150.00m
        };

        // ACT: When I update the event with new details
        var updateResponse = await _adminClient.PutAsJsonAsync($"/admin/events/{eventId}", updateRequest);
        
        // ASSERT: Then the operation is successful
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // ASSERT: And the event reflects the new data
        var seatmapResponse = await _client.GetAsync($"/events/{eventId}/seatmap");
        seatmapResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var seatmapJson = await seatmapResponse.Content.ReadAsStringAsync();
        var seatmap = JsonSerializer.Deserialize<GetEventSeatmapResponse>(seatmapJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        seatmap.Should().NotBeNull();
        seatmap!.EventId.Should().Be(eventId);
        seatmap.EventName.Should().Be("Tech Conference 2025 Advanced");
    }

    [Fact] 
    public async Task T106B_DeactivateEventWithoutActiveReservations_DeactivatesSuccessfully()
    {
        // ARRANGE: Given I have an event "Music Festival" without active reservations
        var eventId = await CreateTestEventWithSeats("Music Festival", "Great music festival");

        // ACT: When I deactivate the event
        var deactivateResponse = await _adminClient.PostAsync($"/admin/events/{eventId}/deactivate", null);
        
        // ASSERT: Then the operation is successful
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // ASSERT: And all seats have status "unavailable"
        var seatmapResponse = await _client.GetAsync($"/events/{eventId}/seatmap");
        seatmapResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var seatmapJson = await seatmapResponse.Content.ReadAsStringAsync();
        var seatmap = JsonSerializer.Deserialize<GetEventSeatmapResponse>(seatmapJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        seatmap.Should().NotBeNull();
        seatmap!.Seats.Should().OnlyContain(seat => seat.Status == "unavailable");
    }

    [Fact]
    public async Task T106C_ReactivateEventWithFutureDate_ReactivatesSuccessfully()
    {
        // ARRANGE: Given I have a deactivated event with future date
        var eventId = await CreateTestEventWithSeats("Future Concert", "Concert in the future", DateTime.UtcNow.AddMonths(2));
        
        // Deactivate first
        var deactivateResponse = await _adminClient.PostAsync($"/admin/events/{eventId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // ACT: When I reactivate the event
        var reactivateResponse = await _adminClient.PostAsync($"/admin/events/{eventId}/reactivate", null);
        
        // ASSERT: Then the operation is successful
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // ASSERT: And all seats have status "available"
        var seatmapResponse = await _client.GetAsync($"/events/{eventId}/seatmap");
        seatmapResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var seatmapJson = await seatmapResponse.Content.ReadAsStringAsync();
        var seatmap = JsonSerializer.Deserialize<GetEventSeatmapResponse>(seatmapJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        seatmap.Should().NotBeNull();
        seatmap!.Seats.Should().OnlyContain(seat => seat.Status == "available");
    }

    [Fact]
    public async Task T106D_DeactivateEventWithActiveReservations_ReturnsBadRequest()
    {
        // ARRANGE: Given I have an event "Popular Concert" 
        var eventId = await CreateTestEventWithSeats("Popular Concert", "Very popular concert");

        // Simulate active reservations by creating reservations in database
        await SimulateActiveReservationsForEvent(eventId);

        // ACT: When I try to deactivate the event
        var deactivateResponse = await _adminClient.PostAsync($"/admin/events/{eventId}/deactivate", null);
        
        // ASSERT: Then I receive error 400 BadRequest
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // ASSERT: And the message indicates "No se puede desactivar un evento con reservas activas"
        var errorContent = await deactivateResponse.Content.ReadAsStringAsync();
        errorContent.Should().Contain("No se puede desactivar un evento con reservas activas");
    }

    [Fact]
    public async Task T106E_ReactivateEventWithPastDate_ReturnsBadRequest()
    {
        // ARRANGE: Given I have a deactivated event - create with future date first
        var eventId = await CreateTestEventWithSeats("Past Concert", "Concert that will be in the past");
        
        // Deactivate the event first
        var deactivateResponse = await _adminClient.PostAsync($"/admin/events/{eventId}/deactivate", null);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Now manually set the event date to the past in the database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var eventEntity = await context.Events.FirstAsync(e => e.Id == eventId);
        
        // Use reflection to set the private EventDate property to past date
        var eventDateProperty = typeof(Event).GetProperty("EventDate");
        eventDateProperty!.SetValue(eventEntity, DateTime.UtcNow.AddDays(-10));
        await context.SaveChangesAsync();

        // ACT: When I try to reactivate the event
        var reactivateResponse = await _adminClient.PostAsync($"/admin/events/{eventId}/reactivate", null);
        
        // ASSERT: Then I receive error 400 BadRequest
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // ASSERT: And the message indicates "No se puede reactivar un evento que ya pasó"
        var errorContent = await reactivateResponse.Content.ReadAsStringAsync();
        errorContent.Should().Contain("No se puede reactivar un evento que ya pasó");
    }

    [Fact]
    public async Task T106_UpdateNonExistentEvent_ReturnsNotFound()
    {
        // ARRANGE: Non-existent event ID
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new 
        {
            Name = "Updated Event",
            Description = "Updated description",
            MaxCapacity = 1000,
            BasePrice = 100.00m
        };

        // ACT & ASSERT: Should return 404 Not Found
        var response = await _adminClient.PutAsJsonAsync($"/admin/events/{nonExistentId}", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task T106_DeactivateNonExistentEvent_ReturnsNotFound()
    {
        // ARRANGE: Non-existent event ID
        var nonExistentId = Guid.NewGuid();

        // ACT & ASSERT: Should return 404 Not Found
        var response = await _adminClient.PostAsync($"/admin/events/{nonExistentId}/deactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task T106_ReactivateNonExistentEvent_ReturnsNotFound()
    {
        // ARRANGE: Non-existent event ID
        var nonExistentId = Guid.NewGuid();

        // ACT & ASSERT: Should return 404 Not Found
        var response = await _adminClient.PostAsync($"/admin/events/{nonExistentId}/reactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Helper method to create a test event for integration testing.
    /// </summary>
    private async Task<Guid> CreateTestEvent(string name, string description, DateTime? eventDate = null)
    {
        var createEventRequest = new
        {
            Name = name,
            Description = description,
            EventDate = eventDate ?? DateTime.UtcNow.AddMonths(1),
            Venue = "Test Venue",
            MaxCapacity = 1000,
            BasePrice = 100.00m
        };

        var createResponse = await _adminClient.PostAsJsonAsync("/admin/events", createEventRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createResponseJson = await createResponse.Content.ReadAsStringAsync();
        var createdEvent = JsonSerializer.Deserialize<JsonElement>(createResponseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return Guid.Parse(createdEvent.GetProperty("id").GetString()!);
    }

    /// <summary>
    /// Helper method to create a test event with seats for integration testing.
    /// </summary>
    private async Task<Guid> CreateTestEventWithSeats(string name, string description, DateTime? eventDate = null)
    {
        var eventId = await CreateTestEvent(name, description, eventDate);

        // Generate seats for the event
        var generateSeatsRequest = new
        {
            SectionConfigurations = new[]
            {
                new { SectionCode = "A", Rows = 5, SeatsPerRow = 10, PriceMultiplier = 1.0m } // 50 seats
            }
        };

        var generateSeatsResponse = await _adminClient.PostAsJsonAsync($"/admin/events/{eventId}/seats", generateSeatsRequest);
        generateSeatsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return eventId;
    }

    /// <summary>
    /// Simulate active reservations for an event by directly inserting reservation data.
    /// This is needed for testing the "event has active reservations" scenario.
    /// </summary>
    private async Task SimulateActiveReservationsForEvent(Guid eventId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Get first seat for this event
        var seat = await context.Seats.FirstOrDefaultAsync(s => s.EventId == eventId);
        seat.Should().NotBeNull("Event should have seats to create reservations");

        // Create a mock reservation by reserving seat (simulates reserved seat)
        seat!.Reserve();
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Generate a JWT token for an admin user for testing admin endpoints.
    /// </summary>
    private static string GenerateAdminToken()
    {
        const string testJwtKey = "dev-secret-key-minimum-32-chars-required-for-security";
        const string testIssuer = "SpecKit.Identity";
        const string testAudience = "SpecKit.Services";
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(testJwtKey);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "test-admin-123"),
            new Claim(JwtRegisteredClaimNames.Email, "admin@test.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = testIssuer,
            Audience = testAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(descriptor);
        return tokenHandler.WriteToken(token);
    }
}