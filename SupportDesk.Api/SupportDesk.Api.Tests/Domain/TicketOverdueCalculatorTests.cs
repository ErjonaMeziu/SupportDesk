using SupportDesk.Api.Domain.Enums;
using SupportDesk.Api.Domain.Rules;
using Xunit;

namespace SupportDesk.Api.Tests.Domain;

public class TicketOverdueCalculatorTests
{
    private static readonly DateTime Now = new(2026, 9, 2, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(TicketStatus.New)]
    [InlineData(TicketStatus.InProgress)]
    public void IsOverdue_WhenDueDateHasPassedAndTicketIsOpen_ReturnsTrue(TicketStatus status)
    {
        var dueDate = Now.AddHours(-1);

        Assert.True(TicketOverdueCalculator.IsOverdue(dueDate, status, Now));
    }

    [Theory]
    [InlineData(TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed)]
    public void IsOverdue_EvenWithAPastDueDate_IsFalseOnceResolvedOrClosed(TicketStatus status)
    {
        var dueDate = Now.AddDays(-5);

        Assert.False(TicketOverdueCalculator.IsOverdue(dueDate, status, Now));
    }

    [Fact]
    public void IsOverdue_WhenDueDateIsInTheFuture_ReturnsFalse()
    {
        var dueDate = Now.AddHours(1);

        Assert.False(TicketOverdueCalculator.IsOverdue(dueDate, TicketStatus.New, Now));
    }
}
