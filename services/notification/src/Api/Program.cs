using Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

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

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.UseInfrastructure();

Console.WriteLine("🚀 Notification API is starting...");
Console.WriteLine("📍 Listening on http://0.0.0.0:5005");
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
