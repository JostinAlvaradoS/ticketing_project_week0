namespace Waitlist.Application.Exceptions;

public class WaitlistConflictException : Exception
{
    public WaitlistConflictException(string message) : base(message) { }
}
