using MediatR;
using Waitlist.Infrastructure;
using Waitlist.Infrastructure.Persistence;
using Waitlist.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(
        typeof(Waitlist.Application.UseCases.JoinWaitlist.JoinWaitlistCommand).Assembly));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()));

builder.Services.Configure<HostOptions>(o => o.StartupTimeout = TimeSpan.FromSeconds(120));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontendPolicy");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapWaitlistEndpoints();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<WaitlistDbContext>();
        db.Database.Migrate();
        Console.WriteLine("✅ Waitlist migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Warning: Could not apply migrations: {ex.Message}");
    }
}

app.Run();

public partial class Program { }
