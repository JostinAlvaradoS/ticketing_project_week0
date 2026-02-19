using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Ports.Inbound;
using PaymentService.Application.Ports.Outbound;
using PaymentService.Application.UseCases;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ITicketHistoryRepository, TicketHistoryRepository>();

builder.Services.AddScoped<IProcessPaymentApprovedUseCase, ProcessPaymentApprovedUseCase>();
builder.Services.AddScoped<IProcessPaymentRejectedUseCase, ProcessPaymentRejectedUseCase>();

builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddScoped<PaymentApprovedEventHandler>();
builder.Services.AddScoped<PaymentRejectedEventHandler>();

builder.Services.AddHostedService<TicketPaymentConsumer>();

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
