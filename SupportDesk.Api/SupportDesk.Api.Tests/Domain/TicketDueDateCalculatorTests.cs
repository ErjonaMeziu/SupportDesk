using SupportDesk.Api.Domain.Enums;
using SupportDesk.Api.Domain.Rules;
using Xunit;

namespace SupportDesk.Api.Tests.Domain;

public class TicketDueDateCalculatorTests
{
    [Theory]
    [InlineData(TicketPriority.Critical, 0, 4, 0)]
    [InlineData(TicketPriority.High, 1, 0, 0)]
    [InlineData(TicketPriority.Normal, 3, 0, 0)]
    [InlineData(TicketPriority.Low, 7, 0, 0)]
    public void Calculate_AddsTheOffsetDefinedForEachPriority(TicketPriority priority, int days, int hours, int minutes)
    {
        var created = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

        var dueDate = TicketDueDateCalculator.Calculate(created, priority);

        Assert.Equal(created.AddDays(days).AddHours(hours).AddMinutes(minutes), dueDate);
    }

    [Fact]
    public void Calculate_WhenPriorityChanges_RecalculatesFromTheOriginalCreatedDate_NotFromNow()
    {
        // Matches the worked example in the assignment: a Normal ticket created 2026-09-01 10:00
        // is due 2026-09-04 10:00. If its priority becomes Critical on 2026-09-02 15:00, the new
        // due date must be 2026-09-01 14:00 (CreatedDate + 4h) - never CreatedDate-of-the-edit + 4h.
        var createdDate = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

        var originalDueDate = TicketDueDateCalculator.Calculate(createdDate, TicketPriority.Normal);
        Assert.Equal(new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc), originalDueDate);

        var recalculatedDueDate = TicketDueDateCalculator.Calculate(createdDate, TicketPriority.Critical);

        Assert.Equal(new DateTime(2026, 9, 1, 14, 0, 0, DateTimeKind.Utc), recalculatedDueDate);
        Assert.NotEqual(new DateTime(2026, 9, 2, 19, 0, 0, DateTimeKind.Utc), recalculatedDueDate);
    }
}
