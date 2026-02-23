using Microsoft.EntityFrameworkCore;
using Identity.Infrastructure;
// Registrar DbContext para migraciones
builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default") ??
        "Host=localhost;Port=5432;Database=ticketing;Username=postgres;Password=postgres",
        npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_identity")
    );
});
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "Identity")
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource("Identity.Api")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("Identity.Api"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddJaegerExporter(options =>
            {
                options.AgentHost = builder.Configuration["Jaeger:AgentHost"] ?? "localhost";
                options.AgentPort = int.Parse(builder.Configuration["Jaeger:AgentPort"] ?? "6831");
            });
    });

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT settings
var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-secret-key-minimum-32-chars-required-for-security";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SpecKit.Identity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SpecKit.Services";

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Health endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Identity",
    timestamp = DateTime.UtcNow
}))
.WithName("Health")
.WithDescription("Health check endpoint");

// Token endpoint - Development JWT
app.MapPost("/token", ([FromBody] TokenRequest request) =>
{
    try
    {
        Log.Information("Token request received for user: {UserId}", request.UserId ?? "guest");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(jwtKey);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.UserId ?? Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add optional email claim
        if (!string.IsNullOrEmpty(request.Email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, request.Email));
        }

        // Add custom claims
        if (request.Claims != null)
        {
            foreach (var claim in request.Claims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(request.ExpiresInHours ?? 24),
            Issuer = jwtIssuer,
            Audience = jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        Log.Information("Token issued successfully for user: {UserId}, expires: {ExpiresAt}",
            request.UserId ?? "guest",
            tokenDescriptor.Expires);

        return Results.Ok(new TokenResponse
        {
            Token = tokenString,
            ExpiresAt = tokenDescriptor.Expires.Value,
            TokenType = "Bearer"
        });
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to issue token for user: {UserId}", request.UserId);
        return Results.Problem(
            detail: "Failed to generate token",
            statusCode: 500
        );
    }
})
.WithName("IssueToken")
.WithDescription("Issue a development JWT token");

app.Run();

// Request/Response DTOs
public record TokenRequest
{
    public string? UserId { get; init; }
    public string? Email { get; init; }
    public int? ExpiresInHours { get; init; }
    public Dictionary<string, string>? Claims { get; init; }
}

public record TokenResponse
{
    public required string Token { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
}
