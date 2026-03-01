using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
using Xunit;

namespace Catalog.IntegrationTests;

/// <summary>
/// T105 Integration Test: Flujo completo Admin - Crear Evento → Generar Asientos → Verificar Read Model
/// 
/// This test validates the complete admin catalog flow as defined in catalog-admin.feature:
/// 
/// Escenario: Flujo completo Admin - Crear Evento → Generar Asientos → Verificar Read Model (T105)
///   Dado que soy un administrador autenticado con Testcontainers ejecutándose
///   Cuando creo un evento "Rock Festival 2026"
///   Y genero 1000 asientos distribuidos en 4 secciones
///   Entonces puedo consultar el evento a través del endpoint público GET /events/{id}/seatmap
///   Y la respuesta contiene:
///     | event_id | coincide con el ID del evento creado |
///     | event_name | Rock Festival 2026 |
///     | seats | array de 1000 asientos |
///     | all_seats_status | available |
///   Y cada asiento tiene:
///     | id | GUID válido |
///     | section_code | A, B, C, o D |
///     | row_number | 1-25 |
///     | seat_number | 1-10 |
///     | price | precio calculado según sección |
///     | status | available |
///
/// DEPENDENCIES:
/// - T100: ✅ Gherkin scenarios (COMPLETED)
/// - T101: ✅ Domain Event.Create() validation (COMPLETED)
/// - T102: ✅ CreateEventCommandHandler (COMPLETED)
/// - T103: ✅ GenerateSeatsCommand bulk generation (COMPLETED)
/// - T104: ✅ Admin endpoints with RequireAdmin policy (COMPLETED)
/// </summary>
[Trait("Category", "IntegrationTest")]
[Trait("Task", "T105")]
public class T105_CatalogAdminFlowIntegrationTests : IAsyncLifetime
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
            .WithDatabase("catalog_integration_test")
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
    public async Task T105_AdminFlow_CreateEventGenerateSeatsVerifyReadModel_CompletesSuccessfully()
    {
        // ARRANGE: Given I am an authenticated admin with Testcontainers running
        var createEventRequest = new
        {
            Name = "Rock Festival 2026",
            Description = "The biggest rock festival of the year",
            EventDate = DateTime.UtcNow.AddMonths(3),
            Venue = "National Stadium",
            MaxCapacity = 1000,
            BasePrice = 100.00m
        };

        // ACT 1: When I create an event "Rock Festival 2026"
        var createResponse = await _adminClient.PostAsJsonAsync("/admin/events", createEventRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createResponseJson = await createResponse.Content.ReadAsStringAsync();
        var createdEvent = JsonSerializer.Deserialize<JsonElement>(createResponseJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var eventId = Guid.Parse(createdEvent.GetProperty("id").GetString()!);
        eventId.Should().NotBe(Guid.Empty);

        // ACT 2: And I generate 1000 seats distributed in 4 sections (A, B, C, D)
        var generateSeatsRequest = new
        {
            SectionConfigurations = new[]
            {
                new { SectionCode = "A", Rows = 25, SeatsPerRow = 10, PriceMultiplier = 2.0m }, // VIP: 250 seats
                new { SectionCode = "B", Rows = 25, SeatsPerRow = 10, PriceMultiplier = 1.5m }, // Premium: 250 seats  
                new { SectionCode = "C", Rows = 25, SeatsPerRow = 10, PriceMultiplier = 1.2m }, // Standard: 250 seats
                new { SectionCode = "D", Rows = 25, SeatsPerRow = 10, PriceMultiplier = 1.0m }  // General: 250 seats
            }
        };

        var generateSeatsResponse = await _adminClient.PostAsJsonAsync($"/admin/events/{eventId}/seats", generateSeatsRequest);
        generateSeatsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // ACT 3: Then I can query the event via public GET /events/{id}/seatmap endpoint
        var seatmapResponse = await _client.GetAsync($"/events/{eventId}/seatmap");
        seatmapResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var seatmapJson = await seatmapResponse.Content.ReadAsStringAsync();
        var seatmap = JsonSerializer.Deserialize<GetEventSeatmapResponse>(seatmapJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // ASSERT: Validate the response contains expected event data
        seatmap.Should().NotBeNull();
        seatmap!.EventId.Should().Be(eventId); // event_id matches created event ID
        seatmap.EventName.Should().Be("Rock Festival 2026"); // event_name matches
        seatmap.Seats.Should().NotBeNull();
        seatmap.Seats.Should().HaveCount(1000); // seats array has 1000 elements

        // ASSERT: All seats should have status "available"
        seatmap.Seats.Should().OnlyContain(seat => seat.Status == "available");

        // ASSERT: Validate each seat has required properties
        foreach (var seat in seatmap.Seats)
        {
            // id: GUID válido
            seat.Id.Should().NotBe(Guid.Empty);
            
            // section_code: A, B, C, or D
            seat.SectionCode.Should().BeOneOf("A", "B", "C", "D");
            
            // row_number: 1-25
            seat.RowNumber.Should().BeInRange(1, 25);
            
            // seat_number: 1-10  
            seat.SeatNumber.Should().BeInRange(1, 10);
            
            // status: available
            seat.Status.Should().Be("available");
            
            // price: calculated according to section
            ValidatePriceBySection(seat.SectionCode, seat.Price, createEventRequest.BasePrice);
        }

        // ASSERT: Validate distribution across sections (250 seats per section)
        var seatsBySection = seatmap.Seats.GroupBy(s => s.SectionCode).ToList();
        seatsBySection.Should().HaveCount(4); // 4 sections
        
        foreach (var sectionGroup in seatsBySection)
        {
            sectionGroup.Should().HaveCount(250, $"Section {sectionGroup.Key} should have 250 seats");
        }

        // ASSERT: Validate row and seat distribution within each section
        foreach (var sectionGroup in seatsBySection)
        {
            var rowGroups = sectionGroup.GroupBy(s => s.RowNumber).ToList();
            rowGroups.Should().HaveCount(25, $"Section {sectionGroup.Key} should have 25 rows");
            
            foreach (var rowGroup in rowGroups)
            {
                rowGroup.Should().HaveCount(10, $"Row {rowGroup.Key} in section {sectionGroup.Key} should have 10 seats");
                
                var seatNumbers = rowGroup.Select(s => s.SeatNumber).OrderBy(x => x).ToList();
                seatNumbers.Should().BeEquivalentTo(Enumerable.Range(1, 10), $"Row {rowGroup.Key} should have seats 1-10");
            }
        }
    }

    [Fact]
    public async Task T105_AdminFlow_UnauthorizedAccess_ReturnsUnauthorized()
    {
        // ARRANGE: No authentication token
        var createEventRequest = new
        {
            Name = "Unauthorized Event",
            Description = "This should fail",
            EventDate = DateTime.UtcNow.AddMonths(1),
            Venue = "Test Venue", 
            MaxCapacity = 100,
            BasePrice = 50.00m
        };

        // ACT & ASSERT: Unauthorized request should return 401
        var response = await _client.PostAsJsonAsync("/admin/events", createEventRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task T105_AdminFlow_CustomerAccess_ReturnsForbidden()
    {
        // ARRANGE: Customer token (not admin)
        var customerClient = _factory.CreateClient();
        var customerToken = GenerateCustomerToken();
        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", customerToken);
        
        var createEventRequest = new
        {
            Name = "Customer Event",
            Description = "This should fail",
            EventDate = DateTime.UtcNow.AddMonths(1),
            Venue = "Test Venue",
            MaxCapacity = 100,
            BasePrice = 50.00m
        };

        // ACT & ASSERT: Customer request should return 403 Forbidden  
        var response = await customerClient.PostAsJsonAsync("/admin/events", createEventRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Generate a JWT token for an admin user for testing admin endpoints.
    /// </summary>
    private static string GenerateAdminToken()
    {
        return GenerateToken("admin@test.com", "test-admin-123");
    }

    /// <summary>
    /// Generate a JWT token for a regular customer user.
    /// </summary>
    private static string GenerateCustomerToken()
    {
        return GenerateToken("customer@test.com", "test-customer-456");
    }

    /// <summary>
    /// Generate a JWT token with custom claims.
    /// </summary>
    private static string GenerateToken(string email, string userId)
    {
        const string testJwtKey = "dev-secret-key-minimum-32-chars-required-for-security";
        const string testIssuer = "SpecKit.Identity";
        const string testAudience = "SpecKit.Services";
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(testJwtKey);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
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

    /// <summary>
    /// Validate that the seat price matches the expected calculation based on section and base price.
    /// </summary>
    private static void ValidatePriceBySection(string sectionCode, decimal actualPrice, decimal basePrice)
    {
        var expectedPrice = sectionCode switch
        {
            "A" => basePrice * 2.0m,   // VIP
            "B" => basePrice * 1.5m,   // Premium  
            "C" => basePrice * 1.2m,   // Standard
            "D" => basePrice * 1.0m,   // General
            _ => throw new ArgumentException($"Unknown section code: {sectionCode}")
        };

        actualPrice.Should().Be(expectedPrice, $"Section {sectionCode} should have price {expectedPrice:C}");
    }
}