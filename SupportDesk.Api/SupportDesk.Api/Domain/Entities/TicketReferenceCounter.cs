namespace SupportDesk.Api.Domain.Entities;

/// <summary>
/// Backs reliable, unique, concurrency-safe ticket reference generation (see SqlTicketReferenceGenerator).
/// One row per calendar year holding the last issued sequence number for that year.
/// </summary>
public class TicketReferenceCounter
{
    public int Year { get; set; }
    public int LastNumber { get; set; }
}
