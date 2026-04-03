using Microsoft.Extensions.Hosting;
using Waitlist.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HostOptions>(o =>
    o.StartupTimeout = TimeSpan.FromSeconds(120));

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseCors("FrontendPolicy");
app.UseInfrastructure();

Console.WriteLine("🚀 Waitlist API starting...");
Console.Out.Flush();

app.Run();

public partial class Program { }
