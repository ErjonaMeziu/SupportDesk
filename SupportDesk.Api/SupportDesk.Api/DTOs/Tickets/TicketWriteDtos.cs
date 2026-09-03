using System.ComponentModel.DataAnnotations;
using SupportDesk.Api.Domain.Enums;

namespace SupportDesk.Api.Dtos.Tickets;

/// <summary>
/// Client-editable fields only. Reference, CreatedDate, LastModifiedDate, DueDate, ResolvedDate
/// and ClosedDate are always server-controlled and are intentionally absent from this DTO.
/// </summary>
public record CreateTicketRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string CustomerName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string CustomerEmail { get; init; } = string.Empty;

    [Required]
    public TicketPriority Priority { get; init; }

    /// <summary>Optional: assign an active agent at creation time. Must be null or an active agent's id.</summary>
    public int? AssignedAgentId { get; init; }
}

/// <summary>
/// Status and assignment are changed exclusively through their dedicated endpoints, not here.
/// </summary>
public record UpdateTicketRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 2)]
    public string CustomerName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string CustomerEmail { get; init; } = string.Empty;

    [Required]
    public TicketPriority Priority { get; init; }
}

public record ChangeStatusRequest
{
    [Required]
    public TicketStatus Status { get; init; }
}

public record AssignAgentRequest
{
    /// <summary>Null unassigns the ticket.</summary>
    public int? AgentId { get; init; }
}

public class TicketQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public int? AssignedAgentId { get; set; }
    public bool OverdueOnly { get; set; }
}
