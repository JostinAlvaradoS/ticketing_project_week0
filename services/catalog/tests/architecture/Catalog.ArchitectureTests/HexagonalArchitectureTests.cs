using NetArchTest.Rules;
using Catalog.Domain.Entities;
using Catalog.Infrastructure.Persistence;
using Xunit;

namespace Catalog.ArchitectureTests;

/// <summary>
/// Architecture validation tests for Catalog service using Hexagonal Architecture principles.
/// </summary>
public class HexagonalArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        // Arrange
        var domainAssembly = typeof(Event).Assembly;
        var infrastructureAssembly = typeof(CatalogDbContext).Assembly;

        // Act & Assert
        Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOn(infrastructureAssembly.GetName().Name)
            .Because("Domain layer should be independent of Infrastructure")
            .Check();
    }

    [Fact]
    public void Domain_Should_Not_Have_External_Dependencies()
    {
        // Arrange
        var domainAssembly = typeof(Event).Assembly;

        // Act & Assert - Domain should only depend on .NET Framework
        Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .Because("Domain should not reference EF Core")
            .Check();
    }
}
