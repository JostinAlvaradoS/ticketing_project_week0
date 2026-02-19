using Microsoft.EntityFrameworkCore;
using ReservationService.Infrastructure;
using ReservationService.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqSettings = new RabbitMQSettings();
builder.Configuration.GetSection(RabbitMQSettings.SectionName).Bind(rabbitMqSettings);

builder.Services.AddReservationServiceInfrastructure(
    dbOptions => dbOptions.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")),
    options =>
    {
        options.Host = rabbitMqSettings.Host;
        options.Port = rabbitMqSettings.Port;
        options.Username = rabbitMqSettings.Username;
        options.Password = rabbitMqSettings.Password;
        options.QueueName = rabbitMqSettings.QueueName;
    });

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .Produces(StatusCodes.Status200OK);

app.Run();
