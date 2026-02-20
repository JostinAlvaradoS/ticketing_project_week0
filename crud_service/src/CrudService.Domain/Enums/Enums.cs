namespace CrudService.Domain.Enums;

public enum TicketStatus
{
    Available,
    Reserved,
    Paid,
    Released,
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    Approved,
    Failed,
    Expired
}
