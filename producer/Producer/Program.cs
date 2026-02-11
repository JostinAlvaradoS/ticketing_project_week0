using Producer.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Cargar variables de entorno
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Agregar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar RabbitMQ y sus servicios
builder.Services.AddRabbitMQ(builder.Configuration);

var app = builder.Build();

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithOpenApi()
    .Produces(StatusCodes.Status200OK);

app.Run();
