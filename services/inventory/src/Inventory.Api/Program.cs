using Inventory.Infrastructure;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Ports;
using Inventory.Api.Endpoints;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Registrar infra y adaptadores del servicio Inventory
builder.Services.AddInfrastructure(builder.Configuration);

// Register MediatR for application handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Inventory.Application.UseCases.CreateReservation.CreateReservationCommand).Assembly));

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

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("✅ Inventory migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Warning: Could not apply migrations: {ex.Message}");
    }
}

app.Run();

public partial class Program { }
