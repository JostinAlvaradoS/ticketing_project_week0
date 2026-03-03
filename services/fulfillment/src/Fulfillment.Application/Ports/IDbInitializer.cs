namespace Fulfillment.Application.Ports;

public interface IDbInitializer
{
    Task InitializeAsync();
}
