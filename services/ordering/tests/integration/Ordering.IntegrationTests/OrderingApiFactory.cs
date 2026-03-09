using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Infrastructure.Persistence;

namespace Ordering.IntegrationTests;

public class OrderingApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OrderingDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add In-Memory Database for testing
            services.AddDbContext<OrderingDbContext>(options =>
            {
                options.UseInMemoryDatabase("OrderingTestDb");
            });

            // Ensure the schema is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
