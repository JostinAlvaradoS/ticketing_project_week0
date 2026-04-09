using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Waitlist.Application.Ports;
using Waitlist.Infrastructure.Persistence;

namespace Waitlist.IntegrationTests;

public class WaitlistWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _dbRoot = new();

    public Mock<ICatalogClient>   CatalogMock   { get; } = new();
    public Mock<IOrderingClient>  OrderingMock  { get; } = new();
    public Mock<IInventoryClient> InventoryMock { get; } = new();
    public Mock<IEmailService>    EmailMock     { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            Remove<DbContextOptions<WaitlistDbContext>>(services);
            Remove<WaitlistDbContext>(services);

            services.AddDbContext<WaitlistDbContext>(options =>
                options.UseInMemoryDatabase("WaitlistTests", _dbRoot));

            Replace<ICatalogClient>(services,   CatalogMock.Object);
            Replace<IOrderingClient>(services,  OrderingMock.Object);
            Replace<IInventoryClient>(services, InventoryMock.Object);
            Replace<IEmailService>(services,    EmailMock.Object);

            services.RemoveAll<IHostedService>();
        });
    }

    private static void Remove<T>(IServiceCollection services)
    {
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in descriptors)
            services.Remove(d);
    }

    private static void Replace<T>(IServiceCollection services, T instance) where T : class
    {
        Remove<T>(services);
        services.AddSingleton(instance);
    }
}
