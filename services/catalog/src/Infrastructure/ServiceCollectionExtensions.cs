using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Catalog.Application.Ports;
using Catalog.Application.UseCases.GetEventSeatmap;
using Catalog.Infrastructure.Persistence;

namespace Catalog.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Controllers
        services.AddControllers();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetEventSeatmapHandler).Assembly));

        services.AddDbContext<CatalogDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_catalog"));
        });
        
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IDbInitializer, DbInitializer>();
        
        return services;
    }

    public static async Task<WebApplication> UseInfrastructure(this WebApplication app)
    {
        // Apply migrations / DB initialization on startup
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
                await dbInitializer.InitializeAsync();
                Console.WriteLine("✓ Catalog DB initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Catalog DB initialization failed: {ex.Message}");
                throw;
            }
        }

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCors("FrontendPolicy");
        app.UseRouting();
        app.MapControllers();

        return app;
    }
}