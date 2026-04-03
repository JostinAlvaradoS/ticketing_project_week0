namespace Waitlist.Domain.Exceptions;

public class WaitlistServiceUnavailableException : Exception
{
    public WaitlistServiceUnavailableException(string message, Exception? inner = null)
        : base(message, inner) { }
}
