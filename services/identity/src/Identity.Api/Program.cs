using Identity.Domain.Ports;
using Identity.Application.UseCases.IssueToken;
using Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


// Infraestructura: registrar adaptadores y contexto vía extensión
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IssueTokenHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapPost("/token", async (
    IssueTokenRequest request,
    IssueTokenHandler handler) =>
{
    var result = await handler.Handle(
        new IssueTokenCommand(request.Email));

    return Results.Ok(result);
});

// Aplicar migraciones automáticamente al iniciar la aplicación
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await dbInitializer.InitializeAsync();
}

app.Run();

public record IssueTokenRequest(string Email);