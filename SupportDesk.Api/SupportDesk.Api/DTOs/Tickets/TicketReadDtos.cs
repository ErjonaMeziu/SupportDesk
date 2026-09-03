using SupportDesk.Api.Domain.Enums;
using SupportDesk.Api.Dtos.Agents;
using SupportDesk.Api.Dtos.Comments;

namespace SupportDesk.Api.Dtos.Tickets;

public record TicketListItemDto(
    int Id,
    string Reference,
    string Title,
    string CustomerName,
    TicketPriority Priority,
    TicketStatus Status,
    int? AssignedAgentId,
    string? AssignedAgentName,
    DateTime DueDate,
    bool IsOverdue,
    DateTime CreatedDate);

public record TicketDetailDto(
    int Id,
    string Reference,
    string Title,
    string Description,
    string CustomerName,
    string CustomerEmail,
    TicketPriority Priority,
    TicketStatus Status,
    int? AssignedAgentId,
    AgentSummaryDto? AssignedAgent,
    DateTime CreatedDate,
    DateTime LastModifiedDate,
    DateTime? ResolvedDate,
    DateTime? ClosedDate,
    DateTime DueDate,
    bool IsOverdue,
    IReadOnlyList<string> AllowedNextStatuses,
    IReadOnlyList<CommentDto> Comments);
