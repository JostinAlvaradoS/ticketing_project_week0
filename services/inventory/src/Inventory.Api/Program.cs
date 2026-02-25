using Inventory.Infrastructure;
using Inventory.Domain.Ports;
using Inventory.Api.Endpoints;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Registrar infra y adaptadores del servicio Inventory
builder.Services.AddInfrastructure(builder.Configuration);

// Register MediatR for application handlers
builder.Services.AddMediatR(typeof(Inventory.Application.UseCases.CreateReservation.CreateReservationCommand).Assembly);

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Map endpoints
app.MapReservationEndpoints();

// Aplicar migraciones / inicialización de BD al iniciar
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
        Console.WriteLine("✓ Inventory DB initialized");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Inventory DB initialization failed: {ex.Message}");
        throw;
    }
}

app.Run();

public partial class Program { }
