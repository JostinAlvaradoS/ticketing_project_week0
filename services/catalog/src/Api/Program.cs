using Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add all services via Infrastructure composition root
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.UseInfrastructure();

app.Run();

public partial class Program { }
