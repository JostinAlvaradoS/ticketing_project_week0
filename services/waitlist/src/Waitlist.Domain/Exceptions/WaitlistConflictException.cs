namespace Waitlist.Domain.Exceptions;

public class WaitlistConflictException : Exception
{
    public WaitlistConflictException(string message) : base(message) { }
}
