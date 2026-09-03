using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Common.Exceptions;
using SupportDesk.Api.Data;
using SupportDesk.Api.Domain.Entities;
using SupportDesk.Api.Domain.Enums;
using SupportDesk.Api.Domain.Rules;
using SupportDesk.Api.Dtos.Agents;
using SupportDesk.Api.Dtos.Comments;
using SupportDesk.Api.Dtos.Common;
using SupportDesk.Api.Dtos.Tickets;

namespace SupportDesk.Api.Services;

/// <summary>
/// Owns every ticket workflow rule. Controllers stay thin and simply call into this service;
/// the actual due-date, status-transition, assignment and read-only rules live in
/// SupportDesk.Api.Domain.Rules and are applied consistently from every mutation method below.
/// </summary>
public class TicketService : ITicketService
{
    private readonly SupportDeskDbContext _db;
    private readonly ITicketReferenceGenerator _referenceGenerator;
    private readonly TimeProvider _timeProvider;

    public TicketService(SupportDeskDbContext db, ITicketReferenceGenerator referenceGenerator, TimeProvider timeProvider)
    {
        _db = db;
        _referenceGenerator = referenceGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<TicketListItemDto>> GetTicketsAsync(TicketQueryParameters query, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var tickets = _db.Tickets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            tickets = tickets.Where(t =>
                t.Reference.Contains(term) ||
                t.Title.Contains(term) ||
                t.CustomerName.Contains(term));
        }

        if (query.Status is not null)
        {
            tickets = tickets.Where(t => t.Status == query.Status);
        }

        if (query.Priority is not null)
        {
            tickets = tickets.Where(t => t.Priority == query.Priority);
        }

        if (query.AssignedAgentId is not null)
        {
            tickets = tickets.Where(t => t.AssignedAgentId == query.AssignedAgentId);
        }

        if (query.OverdueOnly)
        {
            tickets = tickets.Where(t => t.DueDate < now && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed);
        }

        var totalCount = await tickets.CountAsync(cancellationToken);

        var pageOfTickets = await tickets
            .OrderByDescending(t => t.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.Reference,
                t.Title,
                t.CustomerName,
                t.Priority,
                t.Status,
                t.AssignedAgentId,
                AssignedAgentName = t.AssignedAgent == null ? null : t.AssignedAgent.FullName,
                t.DueDate,
                t.CreatedDate
            })
            .ToListAsync(cancellationToken);

        var items = pageOfTickets
            .Select(t => new TicketListItemDto(
                t.Id, t.Reference, t.Title, t.CustomerName, t.Priority, t.Status,
                t.AssignedAgentId, t.AssignedAgentName, t.DueDate,
                TicketOverdueCalculator.IsOverdue(t.DueDate, t.Status, now), t.CreatedDate))
            .ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<TicketListItemDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<TicketDetailDto> GetTicketByIdAsync(int id, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketOrThrowAsync(id, cancellationToken);
        return MapToDetailDto(ticket);
    }

    public async Task<TicketDetailDto> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        Agent? agent = null;
        if (request.AssignedAgentId is int agentId)
        {
            agent = await GetActiveAgentOrThrowAsync(agentId, cancellationToken);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var reference = await _referenceGenerator.GenerateAsync(now.Year, cancellationToken);

        var ticket = new Ticket
        {
            Reference = reference,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = request.CustomerEmail.Trim(),
            Priority = request.Priority,
            Status = TicketStatus.New,
            AssignedAgent = agent,
            CreatedDate = now,
            LastModifiedDate = now,
            DueDate = TicketDueDateCalculator.Calculate(now, request.Priority)
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetTicketByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketDetailDto> UpdateTicketAsync(int id, UpdateTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketOrThrowAsync(id, cancellationToken);
        EnsureNotClosed(ticket);

        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description.Trim();
        ticket.CustomerName = request.CustomerName.Trim();
        ticket.CustomerEmail = request.CustomerEmail.Trim();

        if (ticket.Priority != request.Priority)
        {
            ticket.Priority = request.Priority;
            // Recalculate from the original CreatedDate, never from "now" - see TicketDueDateCalculator.
            ticket.DueDate = TicketDueDateCalculator.Calculate(ticket.CreatedDate, request.Priority);
        }

        ticket.LastModifiedDate = _timeProvider.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(ticket);
    }

    public async Task DeleteTicketAsync(int id, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets.FindAsync(new object?[] { id }, cancellationToken)
            ?? throw new NotFoundException("TICKET_NOT_FOUND", $"Ticket {id} was not found.");

        // Comments are owned by the ticket (cascade delete configured in SupportDeskDbContext)
        // and are removed with it; there is no separate retention requirement for this tool.
        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<TicketDetailDto> ChangeStatusAsync(int id, ChangeStatusRequest request, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketOrThrowAsync(id, cancellationToken);

        if (!TicketStatusRules.CanTransition(ticket.Status, request.Status))
        {
            throw new BusinessRuleException(
                "INVALID_STATUS_TRANSITION",
                $"A ticket in '{ticket.Status}' status cannot transition to '{request.Status}'.");
        }

        if (request.Status == TicketStatus.InProgress)
        {
            if (ticket.AssignedAgentId is null || ticket.AssignedAgent is null)
            {
                throw new BusinessRuleException(
                    "AGENT_REQUIRED_FOR_IN_PROGRESS",
                    "A ticket must have an assigned agent before it can move to In Progress.");
            }

            if (!ticket.AssignedAgent.Active)
            {
                throw new BusinessRuleException(
                    "INACTIVE_AGENT",
                    "The agent assigned to this ticket is inactive and cannot take it into In Progress.");
            }
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (request.Status == TicketStatus.Resolved)
        {
            ticket.ResolvedDate = now;
        }
        else if (request.Status == TicketStatus.Closed)
        {
            ticket.ClosedDate = now;
        }
        else if (request.Status == TicketStatus.InProgress && ticket.Status == TicketStatus.Resolved)
        {
            // Reopening a resolved ticket: ResolvedDate no longer reflects reality, so it is
            // cleared. It will be set again if/when the ticket is resolved a second time.
            // See README "Resolved -> In Progress" for the reasoning.
            ticket.ResolvedDate = null;
        }

        ticket.Status = request.Status;
        ticket.LastModifiedDate = now;

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(ticket);
    }

    public async Task<TicketDetailDto> AssignAgentAsync(int id, AssignAgentRequest request, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketOrThrowAsync(id, cancellationToken);
        EnsureNotClosed(ticket);

        if (request.AgentId is int agentId)
        {
            var agent = await GetActiveAgentOrThrowAsync(agentId, cancellationToken);
            ticket.AssignedAgent = agent;
        }
        else
        {
            ticket.AssignedAgent = null;
        }

        ticket.LastModifiedDate = _timeProvider.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDetailDto(ticket);
    }

    public async Task<CommentDto> AddCommentAsync(int id, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var ticket = await LoadTicketOrThrowAsync(id, cancellationToken);
        EnsureNotClosed(ticket);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var comment = new Comment
        {
            TicketId = ticket.Id,
            AuthorName = request.AuthorName.Trim(),
            Body = request.Body.Trim(),
            CreatedDate = now
        };

        _db.Comments.Add(comment);
        ticket.LastModifiedDate = now;

        await _db.SaveChangesAsync(cancellationToken);

        return new CommentDto(comment.Id, comment.AuthorName, comment.Body, comment.CreatedDate);
    }

    private async Task<Agent> GetActiveAgentOrThrowAsync(int agentId, CancellationToken cancellationToken)
    {
        var agent = await _db.Agents.FindAsync(new object?[] { agentId }, cancellationToken)
            ?? throw new NotFoundException("AGENT_NOT_FOUND", $"Agent {agentId} was not found.");

        if (!agent.Active)
        {
            throw new BusinessRuleException("INACTIVE_AGENT", "Inactive agents cannot be assigned to tickets.");
        }

        return agent;
    }

    private async Task<Ticket> LoadTicketOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var ticket = await _db.Tickets
            .Include(t => t.AssignedAgent)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return ticket ?? throw new NotFoundException("TICKET_NOT_FOUND", $"Ticket {id} was not found.");
    }

    private static void EnsureNotClosed(Ticket ticket)
    {
        if (ticket.Status == TicketStatus.Closed)
        {
            throw new BusinessRuleException("TICKET_CLOSED", "Closed tickets are read-only and cannot be modified.");
        }
    }

    private TicketDetailDto MapToDetailDto(Ticket ticket)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return new TicketDetailDto(
            ticket.Id,
            ticket.Reference,
            ticket.Title,
            ticket.Description,
            ticket.CustomerName,
            ticket.CustomerEmail,
            ticket.Priority,
            ticket.Status,
            ticket.AssignedAgentId,
            ticket.AssignedAgent is null ? null : new AgentSummaryDto(ticket.AssignedAgent.Id, ticket.AssignedAgent.FullName, ticket.AssignedAgent.Active),
            ticket.CreatedDate,
            ticket.LastModifiedDate,
            ticket.ResolvedDate,
            ticket.ClosedDate,
            ticket.DueDate,
            TicketOverdueCalculator.IsOverdue(ticket.DueDate, ticket.Status, now),
            TicketStatusRules.GetAllowedNextStatuses(ticket.Status).Select(s => s.ToString()).ToList(),
            ticket.Comments
                .OrderBy(c => c.CreatedDate)
                .Select(c => new CommentDto(c.Id, c.AuthorName, c.Body, c.CreatedDate))
                .ToList());
    }
}
