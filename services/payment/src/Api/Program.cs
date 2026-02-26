using Payment.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add all services via Infrastructure composition root
builder.Services.AddInfrastructure(builder.Configuration);

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

await app.UseInfrastructure();

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
