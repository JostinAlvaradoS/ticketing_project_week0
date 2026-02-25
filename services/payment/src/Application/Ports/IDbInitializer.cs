namespace Payment.Application.Ports;

public interface IDbInitializer
{
    Task InitializeAsync();
}