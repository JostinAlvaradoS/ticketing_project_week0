using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Catalog.Infrastructure.Persistence;
using Catalog.Application.UseCases.GetEventSeatmap;
using Catalog.Application.Ports;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Configure database 
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register repositories
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetEventSeatmapHandler).Assembly));

var app = builder.Build();

app.MapGet("/health", () => "Catalog service healthy");

// Events API
app.MapGet("/events/{id}/seatmap", async (Guid id, IMediator mediator) =>
{
    var query = new GetEventSeatmapQuery(id);
    var result = await mediator.Send(query);
    
    return result is not null ? Results.Ok(result) : Results.NotFound();
});

app.Run();
