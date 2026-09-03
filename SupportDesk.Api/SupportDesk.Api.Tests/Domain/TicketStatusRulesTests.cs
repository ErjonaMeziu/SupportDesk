using SupportDesk.Api.Domain.Enums;
using SupportDesk.Api.Domain.Rules;
using Xunit;

namespace SupportDesk.Api.Tests.Domain;

public class TicketStatusRulesTests
{
    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.InProgress)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress)]
    public void CanTransition_AllowsTheDocumentedWorkflowTransitions(TicketStatus from, TicketStatus to)
    {
        Assert.True(TicketStatusRules.CanTransition(from, to));
    }

    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Resolved)]
    [InlineData(TicketStatus.New, TicketStatus.Closed)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Closed)]
    [InlineData(TicketStatus.InProgress, TicketStatus.New)]
    [InlineData(TicketStatus.Resolved, TicketStatus.New)]
    [InlineData(TicketStatus.Closed, TicketStatus.New)]
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Closed, TicketStatus.Resolved)]
    public void CanTransition_RejectsEverythingElse(TicketStatus from, TicketStatus to)
    {
        Assert.False(TicketStatusRules.CanTransition(from, to));
    }

    [Fact]
    public void CanTransition_AClosedTicketCanNeverBeReopened()
    {
        foreach (TicketStatus target in Enum.GetValues<TicketStatus>())
        {
            Assert.False(TicketStatusRules.CanTransition(TicketStatus.Closed, target));
        }
    }

    [Fact]
    public void GetAllowedNextStatuses_ForAResolvedTicket_OffersBothCloseAndReopen()
    {
        var next = TicketStatusRules.GetAllowedNextStatuses(TicketStatus.Resolved);

        Assert.Equal(new[] { TicketStatus.Closed, TicketStatus.InProgress }, next);
    }
}
