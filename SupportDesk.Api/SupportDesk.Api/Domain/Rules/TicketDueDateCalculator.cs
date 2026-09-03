using SupportDesk.Api.Domain.Enums;

namespace SupportDesk.Api.Domain.Rules;

/// <summary>
/// Single source of truth for the ticket due-date rule (README "Business rules" section).
/// The due date is always derived from the ticket's original CreatedDate and its current
/// priority, never from "now" - so a priority change recalculates from the original creation
/// time instead of extending the deadline from the moment of the edit.
/// </summary>
public static class TicketDueDateCalculator
{
    public static DateTime Calculate(DateTime createdDateUtc, TicketPriority priority) => priority switch
    {
        TicketPriority.Critical => createdDateUtc.AddHours(4),
        TicketPriority.High => createdDateUtc.AddDays(1),
        TicketPriority.Normal => createdDateUtc.AddDays(3),
        TicketPriority.Low => createdDateUtc.AddDays(7),
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown ticket priority.")
    };
}
