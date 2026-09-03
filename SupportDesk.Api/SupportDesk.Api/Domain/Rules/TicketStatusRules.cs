using SupportDesk.Api.Domain.Enums;

namespace SupportDesk.Api.Domain.Rules;

/// <summary>
/// Single source of truth for the ticket status workflow (README "Business rules" section).
/// The only valid transitions are New -> InProgress -> Resolved -> Closed, plus the single
/// "reopen" path Resolved -> InProgress. A Closed ticket has no outgoing transitions.
/// </summary>
public static class TicketStatusRules
{
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.New] = new[] { TicketStatus.InProgress },
        [TicketStatus.InProgress] = new[] { TicketStatus.Resolved },
        [TicketStatus.Resolved] = new[] { TicketStatus.Closed, TicketStatus.InProgress },
        [TicketStatus.Closed] = Array.Empty<TicketStatus>()
    };

    public static bool CanTransition(TicketStatus from, TicketStatus to) =>
        AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static IReadOnlyList<TicketStatus> GetAllowedNextStatuses(TicketStatus from) =>
        AllowedTransitions.TryGetValue(from, out var allowed) ? allowed : Array.Empty<TicketStatus>();
}
