using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Confluent.Kafka;
using Catalog.Application.Ports;
using Catalog.Application.UseCases.GetEventSeatmap;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Messaging;
using Catalog.Infrastructure.Consumers;

namespace Catalog.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Controllers
        services.AddControllers();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetEventSeatmapHandler).Assembly));

        // Add Database Context
        services.AddDbContext<CatalogDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Default"), 
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_catalog"));
        });
        
        // Add Repository
        services.AddScoped<ICatalogRepository, CatalogRepository>();

        // Configure Kafka producer
        var kafkaBootstrapServers = configuration.GetConnectionString("Kafka") ?? configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaConfig = new ProducerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            AllowAutoCreateTopics = true,
            Acks = Acks.All
        };
        
        // Use factory registration to avoid building producer during EF migrations
        services.AddSingleton(sp => new ProducerBuilder<string?, string>(kafkaConfig).Build());
        services.AddSingleton<IKafkaProducer, KafkaProducer>();

        // Kafka consumers
        services.AddHostedService<CatalogEventConsumer>();

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "catalog-ticket-issued-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        
        // Using factory registration for the consumer too
        services.AddSingleton<IHostedService, TicketIssuedConsumer>(sp =>
        {
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var consumer = new ConsumerBuilder<string?, string>(consumerConfig).Build();
            return new TicketIssuedConsumer(scopeFactory, consumer);
        });
        
        // Add JWT Authentication
        var jwtKey = configuration["Jwt:Key"]!;
        var jwtIssuer = configuration["Jwt:Issuer"]!;
        var jwtAudience = configuration["Jwt:Audience"]!;
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        // Add Authorization with Admin policy
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireAssertion(context =>
                {
                    // For MVP: Check if user email contains "admin" or matches test admin
                    var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value;
                    return !string.IsNullOrEmpty(emailClaim) && 
                           (emailClaim.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
                            emailClaim.Equals("test@example.com", StringComparison.OrdinalIgnoreCase));
                }));
        });

        return services;
    }

    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        // Apply migrations automatically on startup
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
                dbContext.Database.Migrate();
                Console.WriteLine("✅ Catalog migrations applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Warning: Could not apply migrations: {ex.Message}");
            }
        }

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCors("FrontendPolicy");
        app.UseRouting();
        
        // Add Authentication & Authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.MapControllers();

        return app;
    }
}