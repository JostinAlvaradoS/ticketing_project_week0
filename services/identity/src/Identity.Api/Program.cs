using Identity.Domain.Ports;
using Identity.Domain.ValueObjects;
using Identity.Application.UseCases.IssueToken;
using Identity.Application.UseCases.CreateUser;
using Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


// Infraestructura: registrar adaptadores y contexto vía extensión
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IssueTokenHandler>();
builder.Services.AddScoped<CreateUserHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "identity" }));

// DB initialization and migrations are now handled externally (pipeline)

app.Run();

public record IssueTokenRequest(string Email, string Password);

public record IssueTokenResponse(
    string token,
    DateTime expiresAt,
    string userRole,
    string userEmail
);