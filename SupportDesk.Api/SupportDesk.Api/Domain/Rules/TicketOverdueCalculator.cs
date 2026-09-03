using SupportDesk.Api.Domain.Enums;

namespace SupportDesk.Api.Domain.Rules;

/// <summary>
/// Single source of truth for the "overdue" definition (README "Business rules" section):
/// a ticket is overdue when its due date has passed and it is not Resolved or Closed.
/// The frontend never computes this itself - it only reflects the IsOverdue flag returned by the API.
/// </summary>
public static class TicketOverdueCalculator
{
    public static bool IsOverdue(DateTime dueDateUtc, TicketStatus status, DateTime nowUtc) =>
        dueDateUtc < nowUtc && status != TicketStatus.Resolved && status != TicketStatus.Closed;
}
