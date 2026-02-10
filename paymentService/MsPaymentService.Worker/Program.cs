using Microsoft.EntityFrameworkCore;
using MsPaymentService.Worker;
using PaymentService.Api.Data;
using PaymentService.Api.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// Configurar DbContext
builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddRepositories();
builder.Services.AddTicketPaymentConsumer();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
