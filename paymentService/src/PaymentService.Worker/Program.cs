using Microsoft.EntityFrameworkCore;
using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqSettings = new RabbitMQSettings();
builder.Configuration.GetSection(RabbitMQSettings.SectionName).Bind(rabbitMqSettings);

builder.Services.AddPaymentServiceInfrastructure(
    dbOptions => dbOptions.UseNpgsql(builder.Configuration.GetConnectionString("TicketingDb")),
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
