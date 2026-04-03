using Identity.Domain.Ports;
using Identity.Domain.ValueObjects;
using Identity.Application.UseCases.IssueToken;
using Identity.Application.UseCases.CreateUser;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Infraestructura: registrar adaptadores y contexto vía extensión
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IssueTokenHandler>();
builder.Services.AddScoped<CreateUserHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JSON serialization to handle enum strings
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add CORS policy for frontend on localhost:3000
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("FrontendPolicy");

app.MapPost("/token", async (
    IssueTokenRequest request,
    IssueTokenHandler handler) =>
{
    try
    {
        var result = await handler.Handle(
            new IssueTokenCommand(request.Email, request.Password));

        // Mapear a la respuesta esperada por el contrato OpenAPI y frontend
        var response = new IssueTokenResponse(
            token: result.AccessToken,
            expiresAt: result.ExpiresAt,
            userRole: result.UserRole.ToString(),
            userEmail: result.UserEmail
        );

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Token generation error: {ex.Message}");
        return Results.Unauthorized();
    }
});

// Create user endpoint
app.MapPost("/users", async (
    CreateUserRequest request,
    CreateUserHandler handler) =>
{
    try
    {
        var userId = await handler.Handle(
            new CreateUserCommand(request.Email, request.Password, request.Role));

        var response = new CreateUserResponse(
            userId: userId,
            email: request.Email,
            role: request.Role.ToString()
        );

        return Results.Created($"/users/{userId}", response);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"User creation error: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "identity" }));

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("✅ Identity migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Warning: Could not apply migrations: {ex.Message}");
    }
}

app.Run();

public record IssueTokenRequest(string Email, string Password);

public record IssueTokenResponse(
    string token,
    DateTime expiresAt,
    string userRole,
    string userEmail
);

public record CreateUserRequest(string Email, string Password, Role Role = Role.User);

public record CreateUserResponse(
    Guid userId,
    string email,
    string role
);

public partial class Program { }