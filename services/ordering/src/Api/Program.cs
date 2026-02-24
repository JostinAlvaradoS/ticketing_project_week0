using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ordering.Infrastructure;
using Ordering.Application.UseCases.AddToCart;
using Ordering.Application.Ports;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AddToCartHandler).Assembly));

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
        Console.WriteLine("✓ Ordering DB initialized");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Warning: Could not initialize database");
        Console.WriteLine($"  Reason: {ex.Message}");
        Console.WriteLine($"  Service will continue to run. DB will be initialized on next startup.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.MapControllers();

Console.WriteLine("🚀 Ordering API is starting...");
Console.WriteLine("📍 Listening on http://0.0.0.0:5003");
Console.Out.Flush();

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Application failed: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}

public partial class Program { }
