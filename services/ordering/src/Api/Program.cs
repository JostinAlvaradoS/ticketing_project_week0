using Ordering.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add all services via Infrastructure composition root
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.UseInfrastructure();

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
