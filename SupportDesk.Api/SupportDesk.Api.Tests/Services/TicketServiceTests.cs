using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Common.Exceptions;
using SupportDesk.Api.Data;
using SupportDesk.Api.Domain.Entities;
using SupportDesk.Api.Domain.Enums;
using SupportDesk.Api.Dtos.Comments;
using SupportDesk.Api.Dtos.Tickets;
using SupportDesk.Api.Services;
using SupportDesk.Api.Tests.Support;
using Xunit;

namespace SupportDesk.Api.Tests.Services;

public class TicketServiceTests : IDisposable
{
    private readonly SupportDeskDbContext _db;
    private readonly FakeTimeProvider _clock;
    private readonly TicketService _sut;

    public TicketServiceTests()
    {
        var options = new DbContextOptionsBuilder<SupportDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new SupportDeskDbContext(options);
        _clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero));
        _sut = new TicketService(_db, new FakeTicketReferenceGenerator(), _clock);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Agent> AddAgentAsync(bool active = true)
    {
        var agent = new Agent { FullName = "Test Agent", Email = $"{Guid.NewGuid()}@example.com", Department = Department.Technical, Active = active };
        _db.Agents.Add(agent);
        await _db.SaveChangesAsync();
        return agent;
    }

    private static CreateTicketRequest ValidCreateRequest(TicketPriority priority = TicketPriority.Normal, int? agentId = null) => new()
    {
        Title = "Cannot access account",
        Description = "Customer reports being locked out after a password reset attempt.",
        CustomerName = "Jane Doe",
        CustomerEmail = "jane.doe@example.com",
        Priority = priority,
        AssignedAgentId = agentId
    };

    [Fact]
    public async Task CreateTicketAsync_CalculatesDueDateFromServerCreatedDateAndPriority()
    {
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(TicketPriority.Critical), CancellationToken.None);

        Assert.Equal(_clock.GetUtcNow().UtcDateTime, ticket.CreatedDate);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime.AddHours(4), ticket.DueDate);
        Assert.Equal(TicketStatus.New, ticket.Status);
        Assert.StartsWith("TCK-", ticket.Reference);
    }

    [Fact]
    public async Task ChangeStatusAsync_NewToInProgress_SucceedsWhenAnActiveAgentIsAssigned()
    {
        var agent = await AddAgentAsync(active: true);
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(agentId: agent.Id), CancellationToken.None);

        var updated = await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }, CancellationToken.None);

        Assert.Equal(TicketStatus.InProgress, updated.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_NewToResolved_IsRejectedAsAnInvalidTransition()
    {
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Resolved }, CancellationToken.None));

        Assert.Equal("INVALID_STATUS_TRANSITION", ex.Code);
    }

    [Fact]
    public async Task ChangeStatusAsync_NewToInProgress_IsRejectedWhenNoAgentIsAssigned()
    {
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }, CancellationToken.None));

        Assert.Equal("AGENT_REQUIRED_FOR_IN_PROGRESS", ex.Code);
    }

    [Fact]
    public async Task ChangeStatusAsync_NewToInProgress_IsRejectedWhenTheAssignedAgentHasSinceBecomeInactive()
    {
        // The agent was active (and assignable) at assignment time, then was deactivated before
        // anyone started work on the ticket - InProgress must re-check the agent's current status.
        var agent = await AddAgentAsync(active: true);
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(agentId: agent.Id), CancellationToken.None);

        agent.Active = false;
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }, CancellationToken.None));

        Assert.Equal("INACTIVE_AGENT", ex.Code);
    }

    [Fact]
    public async Task AssignAgentAsync_RejectsAnInactiveAgent()
    {
        var agent = await AddAgentAsync(active: false);
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.AssignAgentAsync(ticket.Id, new AssignAgentRequest { AgentId = agent.Id }, CancellationToken.None));

        Assert.Equal("INACTIVE_AGENT", ex.Code);
    }

    [Fact]
    public async Task ChangeStatusAsync_ClosedTicketCanNeverBeReopened()
    {
        var agent = await AddAgentAsync();
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(agentId: agent.Id), CancellationToken.None);
        await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }, CancellationToken.None);
        await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Resolved }, CancellationToken.None);
        await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Closed }, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }, CancellationToken.None));

        Assert.Equal("INVALID_STATUS_TRANSITION", ex.Code);
    }

    [Fact]
    public async Task ChangeStatusAsync_InProgressToResolved_SetsResolvedDateFromTheServerClock()
    {
        var agent = await AddAgentAsync();
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(agentId: agent.Id), CancellationToken.None);
        await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }, CancellationToken.None);

        _clock.Set(_clock.GetUtcNow().AddHours(3));
        var resolved = await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Resolved }, CancellationToken.None);

        Assert.Equal(_clock.GetUtcNow().UtcDateTime, resolved.ResolvedDate);
    }

    [Fact]
    public async Task ChangeStatusAsync_ResolvedToInProgress_ClearsResolvedDateBecauseTheTicketIsReopened()
    {
        var agent = await AddAgentAsync();
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(agentId: agent.Id), CancellationToken.None);
        await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }, CancellationToken.None);
        await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Resolved }, CancellationToken.None);

        var reopened = await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }, CancellationToken.None);

        Assert.Equal(TicketStatus.InProgress, reopened.Status);
        Assert.Null(reopened.ResolvedDate);
    }

    [Fact]
    public async Task UpdateTicketAsync_WhenPriorityChanges_RecalculatesDueDateFromTheOriginalCreatedDate()
    {
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(TicketPriority.Normal), CancellationToken.None);
        var originalCreatedDate = ticket.CreatedDate;

        // Time passes before the priority is edited - the due date must still be based on
        // originalCreatedDate, not on the clock value at the moment of this update.
        _clock.Set(_clock.GetUtcNow().AddDays(1).AddHours(5));

        var updated = await _sut.UpdateTicketAsync(ticket.Id, new UpdateTicketRequest
        {
            Title = ticket.Title,
            Description = ticket.Description,
            CustomerName = ticket.CustomerName,
            CustomerEmail = ticket.CustomerEmail,
            Priority = TicketPriority.Critical
        }, CancellationToken.None);

        Assert.Equal(originalCreatedDate.AddHours(4), updated.DueDate);
    }

    [Theory]
    [MemberData(nameof(ClosedTicketMutations))]
    public async Task ClosedTickets_RejectEveryMutation(Func<TicketService, int, Task> mutate)
    {
        var agent = await AddAgentAsync();
        var ticket = await _sut.CreateTicketAsync(ValidCreateRequest(agentId: agent.Id), CancellationToken.None);
        await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.InProgress }, CancellationToken.None);
        await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Resolved }, CancellationToken.None);
        await _sut.ChangeStatusAsync(ticket.Id, new ChangeStatusRequest { Status = TicketStatus.Closed }, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => mutate(_sut, ticket.Id));

        Assert.Equal("TICKET_CLOSED", ex.Code);
    }

    public static IEnumerable<object[]> ClosedTicketMutations()
    {
        yield return new object[]
        {
            (Func<TicketService, int, Task>)((service, id) => service.UpdateTicketAsync(id, new UpdateTicketRequest
            {
                Title = "New title",
                Description = "Some updated description text.",
                CustomerName = "Someone",
                CustomerEmail = "someone@example.com",
                Priority = TicketPriority.Low
            }, CancellationToken.None))
        };
        yield return new object[]
        {
            (Func<TicketService, int, Task>)((service, id) => service.AssignAgentAsync(id, new AssignAgentRequest { AgentId = null }, CancellationToken.None))
        };
        yield return new object[]
        {
            (Func<TicketService, int, Task>)((service, id) => service.AddCommentAsync(id, new CreateCommentRequest { AuthorName = "Someone", Body = "A comment" }, CancellationToken.None))
        };
    }

    [Fact]
    public async Task GetTicketsAsync_OverdueOnlyFilter_ReturnsOnlyOpenTicketsPastTheirDueDate()
    {
        var agent = await AddAgentAsync();

        // Overdue: created far enough in the past for a Low-priority (7 day) SLA to have lapsed.
        _clock.Set(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));
        var overdueTicket = await _sut.CreateTicketAsync(ValidCreateRequest(TicketPriority.Low, agent.Id), CancellationToken.None);

        // Not overdue: created recently under the same Low-priority SLA.
        _clock.Set(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        await _sut.CreateTicketAsync(ValidCreateRequest(TicketPriority.Low), CancellationToken.None);

        _clock.Set(new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero));

        var result = await _sut.GetTicketsAsync(new TicketQueryParameters { OverdueOnly = true, PageSize = 50 }, CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(overdueTicket.Id, item.Id);
        Assert.True(item.IsOverdue);
    }
}
