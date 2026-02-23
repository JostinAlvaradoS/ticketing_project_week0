namespace Inventory.Domain.Ports;

public interface IDbInitializer
{
    Task InitializeAsync();
}
