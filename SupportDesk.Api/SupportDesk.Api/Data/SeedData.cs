using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Domain.Entities;
using SupportDesk.Api.Domain.Enums;
using SupportDesk.Api.Domain.Rules;

namespace SupportDesk.Api.Data;

/// <summary>
/// Deterministic demo dataset: 5 agents (one inactive) and 20 tickets spanning every status,
/// priority and assignment combination, including several tickets that are genuinely overdue
/// under TicketOverdueCalculator. All seeded data respects the same workflow rules the API
/// enforces (e.g. every InProgress ticket has an active assigned agent).
/// </summary>
public static class SeedData
{
    public static async Task SeedAsync(SupportDeskDbContext db)
    {
        if (await db.Agents.AnyAsync() || await db.Tickets.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var marija = new Agent { FullName = "Marija Petrova", Email = "marija.petrova@supportdesk.io", Department = Department.Technical, Active = true };
        var ilir = new Agent { FullName = "Ilir Krasniqi", Email = "ilir.krasniqi@supportdesk.io", Department = Department.Billing, Active = true };
        var sara = new Agent { FullName = "Sara Kovac", Email = "sara.kovac@supportdesk.io", Department = Department.General, Active = true };
        var dritan = new Agent { FullName = "Dritan Hoxha", Email = "dritan.hoxha@supportdesk.io", Department = Department.Technical, Active = true };
        var elira = new Agent { FullName = "Elira Gashi", Email = "elira.gashi@supportdesk.io", Department = Department.Billing, Active = false };

        db.Agents.AddRange(marija, ilir, sara, dritan, elira);

        var tickets = new List<Ticket>
        {
            Ticket("Cannot log into customer portal", TicketPriority.Critical, TicketStatus.New, null, HoursAgo(now, 2)),
            Ticket("Payment gateway returning 500 errors", TicketPriority.Critical, TicketStatus.New, null, DaysAgo(now, 1)),
            Ticket("Production API outage for EU customers", TicketPriority.Critical, TicketStatus.InProgress, marija, HoursAgo(now, 12)),
            Ticket("Invoice PDF attached to wrong account", TicketPriority.High, TicketStatus.New, elira, HoursAgo(now, 5)),
            Ticket("Data export stuck at 0% for 3 days", TicketPriority.High, TicketStatus.InProgress, ilir, DaysAgo(now, 3)),
            ResolvedTicket("SSO login loop after password reset", TicketPriority.High, marija, DaysAgo(now, 5), DaysAgo(now, 1)),
            ClosedTicket("Duplicate charge on subscription renewal", TicketPriority.High, sara, DaysAgo(now, 10), DaysAgo(now, 6), DaysAgo(now, 2)),
            Ticket("Mobile app crashes on ticket attachment upload", TicketPriority.High, TicketStatus.New, null, DaysAgo(now, 6)),
            Ticket("Request to add a new report column", TicketPriority.Normal, TicketStatus.New, null, HoursAgo(now, 12)),
            Ticket("Change billing contact email", TicketPriority.Normal, TicketStatus.New, ilir, DaysAgo(now, 1)),
            Ticket("Slow page load on dashboard widgets", TicketPriority.Normal, TicketStatus.InProgress, marija, DaysAgo(now, 2)),
            Ticket("Onboarding checklist not saving progress", TicketPriority.Normal, TicketStatus.InProgress, dritan, DaysAgo(now, 4)),
            ResolvedTicket("Typo in weekly summary email", TicketPriority.Normal, ilir, DaysAgo(now, 6), DaysAgo(now, 2)),
            ClosedTicket("Add second admin user to account", TicketPriority.Normal, marija, DaysAgo(now, 15), DaysAgo(now, 10), DaysAgo(now, 8)),
            Ticket("Clarify API rate limit documentation", TicketPriority.Normal, TicketStatus.InProgress, sara, HoursAgo(now, 10)),
            Ticket("Feature request: dark mode for reports", TicketPriority.Low, TicketStatus.New, null, DaysAgo(now, 1)),
            Ticket("Minor CSS misalignment on settings page", TicketPriority.Low, TicketStatus.InProgress, dritan, DaysAgo(now, 9)),
            ResolvedTicket("Question about data retention policy", TicketPriority.Low, sara, DaysAgo(now, 12), DaysAgo(now, 3)),
            ClosedTicket("Old trial account cleanup request", TicketPriority.Low, ilir, DaysAgo(now, 20), DaysAgo(now, 14), DaysAgo(now, 10)),
            ResolvedTicket("Confirm refund was processed correctly", TicketPriority.Critical, dritan, DaysAgo(now, 2), DaysAgo(now, 1))
        };

        db.Tickets.AddRange(tickets);
        db.TicketReferenceCounters.Add(new TicketReferenceCounter { Year = now.Year, LastNumber = tickets.Count });

        for (var i = 0; i < tickets.Count; i++)
        {
            tickets[i].Reference = $"TCK-{now.Year}-{i + 1:D4}";
        }

        await db.SaveChangesAsync();
    }

    private static DateTime HoursAgo(DateTime now, double hours) => now.AddHours(-hours);
    private static DateTime DaysAgo(DateTime now, double days) => now.AddDays(-days);

    private static Ticket Ticket(string title, TicketPriority priority, TicketStatus status, Agent? agent, DateTime createdDate)
    {
        return new Ticket
        {
            Title = title,
            Description = $"{title}. Reported by customer via the support portal; needs investigation by the assigned team.",
            CustomerName = RandomCustomerName(title),
            CustomerEmail = $"{RandomCustomerName(title).ToLowerInvariant().Replace(' ', '.')}@example.com",
            Priority = priority,
            Status = status,
            AssignedAgent = agent,
            CreatedDate = createdDate,
            LastModifiedDate = createdDate,
            DueDate = TicketDueDateCalculator.Calculate(createdDate, priority)
        };
    }

    private static Ticket ResolvedTicket(string title, TicketPriority priority, Agent agent, DateTime createdDate, DateTime resolvedDate)
    {
        var ticket = Ticket(title, priority, TicketStatus.Resolved, agent, createdDate);
        ticket.ResolvedDate = resolvedDate;
        ticket.LastModifiedDate = resolvedDate;
        return ticket;
    }

    private static Ticket ClosedTicket(string title, TicketPriority priority, Agent agent, DateTime createdDate, DateTime resolvedDate, DateTime closedDate)
    {
        var ticket = ResolvedTicket(title, priority, agent, createdDate, resolvedDate);
        ticket.Status = TicketStatus.Closed;
        ticket.ClosedDate = closedDate;
        ticket.LastModifiedDate = closedDate;
        return ticket;
    }

    private static string RandomCustomerName(string seedText)
    {
        string[] names =
        {
            "Anna Berisha", "Tom Becker", "Lena Vogel", "Marco Rossi", "Julia Novak",
            "Ben Fischer", "Nora Weiss", "Chris Adler", "Mia Brandt", "Leon Krüger",
            "Ana Marić", "Petar Jovanović", "Klara Schmidt", "Ivo Horvat", "Sven Lund",
            "Rina Toska", "Genc Sula", "Dana Volkov", "Milo Zima", "Ema Bartos"
        };

        var index = Math.Abs(seedText.GetHashCode()) % names.Length;
        return names[index];
    }
}
