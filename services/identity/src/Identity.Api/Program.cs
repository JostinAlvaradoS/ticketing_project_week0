using Identity.Domain.Ports;
using Identity.Application.UseCases.IssueToken;
using Identity.Application.UseCases.CreateUser;
using Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);


// Infraestructura: registrar adaptadores y contexto vía extensión
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IssueTokenHandler>();
builder.Services.AddScoped<CreateUserHandler>();

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

app.UseCors("FrontendPolicy");

app.MapPost("/token", async (
    IssueTokenRequest request,
    IssueTokenHandler handler) =>
{
    var result = await handler.Handle(
        new IssueTokenCommand(request.Email, request.Password));

    return Results.Ok(result);
});

// Aplicar migraciones automáticamente al iniciar la aplicación
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ FATAL: No se pudo inicializar la base de datos");
        Console.WriteLine($"  Razón: {ex.Message}");
        Console.WriteLine($"  Asegúrate de:");
        Console.WriteLine($"    1. Levantar infra: docker-compose up -d");
        Console.WriteLine($"    2. Usar database 'ticketing' en appsettings.json");
        throw;
    }
    
    // Seed: crear usuario de prueba si no existe (solo si BD está ok)
    try
    {
        var createUserHandler = scope.ServiceProvider.GetRequiredService<CreateUserHandler>();
        await createUserHandler.Handle(new CreateUserCommand("test@example.com", "Password123!"));
        Console.WriteLine("✓ Usuario de prueba creado: test@example.com");
    }
    catch (Exception ex)
    {
        if (!ex.Message.Contains("already exists"))
        {
            Console.WriteLine($"⚠ Warning al crear usuario de prueba: {ex.Message}");
        }
        else
        {
            Console.WriteLine("✓ Usuario de prueba ya existe");
        }
    }
}

app.Run();

public record IssueTokenRequest(string Email, string Password);