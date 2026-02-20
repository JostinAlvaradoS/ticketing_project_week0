using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqSettings = new RabbitMQSettings();
builder.Configuration.GetSection(RabbitMQSettings.SectionName).Bind(rabbitMqSettings);

var dataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("TicketingDb"));
dataSourceBuilder.MapEnum<TicketStatus>();
dataSourceBuilder.MapEnum<PaymentStatus>();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddPaymentServiceInfrastructure(
    dbOptions => dbOptions.UseNpgsql(dataSource),
    options =>
    {
        options.Host = rabbitMqSettings.Host;
        options.Port = rabbitMqSettings.Port;
        options.Username = rabbitMqSettings.Username;
        options.Password = rabbitMqSettings.Password;
        options.ApprovedQueueName = rabbitMqSettings.ApprovedQueueName;
        options.RejectedQueueName = rabbitMqSettings.RejectedQueueName;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    dbContext.Database.EnsureCreated();
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("Health")
    .Produces(StatusCodes.Status200OK);

app.Run();
