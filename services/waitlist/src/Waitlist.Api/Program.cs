using Waitlist.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseInfrastructure();

Console.WriteLine("🚀 Waitlist API starting...");
Console.Out.Flush();

app.Run();

public partial class Program { }
