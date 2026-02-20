using Producer.Application.Ports.Inbound;
using Producer.Application.Ports.Outbound;
using Producer.Application.UseCases;
using Producer.Infrastructure.Messaging;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

var rabbitMqSettings = new RabbitMQSettings();
builder.Configuration.GetSection("RabbitMQ").Bind(rabbitMqSettings);
builder.Services.AddSingleton(rabbitMqSettings);

builder.Services.AddSingleton<IConnection>(provider =>
{
    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("RabbitMQ.Configuration");
    var options = new RabbitMQSettings();
    builder.Configuration.GetSection("RabbitMQ").Bind(options);

    logger.LogInformation("Configurando conexión RabbitMQ: Host={Host}, Port={Port}", options.Host, options.Port);

    var factory = new ConnectionFactory
    {
        HostName = options.Host,
        Port = options.Port,
        UserName = options.Username,
        Password = options.Password,
        VirtualHost = options.VirtualHost,
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
        RequestedConnectionTimeout = TimeSpan.FromSeconds(30),
        RequestedHeartbeat = TimeSpan.FromSeconds(10)
    };

    try
    {
        var connection = factory.CreateConnection();
        logger.LogInformation("Conexión RabbitMQ establecida exitosamente");
        return connection;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al conectar con RabbitMQ");
        throw;
    }
});

builder.Services.AddScoped<ITicketEventPublisher, RabbitMQTicketPublisher>();
builder.Services.AddScoped<IPaymentEventPublisher, RabbitMQPaymentPublisher>();

builder.Services.AddScoped<IReserveTicketUseCase, ReserveTicketUseCase>();
builder.Services.AddScoped<IProcessPaymentUseCase, ProcessPaymentUseCase>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .WithOpenApi()
    .Produces(StatusCodes.Status200OK);

app.Run();
