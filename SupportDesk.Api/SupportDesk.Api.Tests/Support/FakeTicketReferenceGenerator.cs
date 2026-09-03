using SupportDesk.Api.Data;

namespace SupportDesk.Api.Tests.Support;

/// <summary>
/// Simple in-memory stand-in for SqlTicketReferenceGenerator. The real generator relies on a
/// SQL Server-specific MERGE + HOLDLOCK statement that has no meaningful equivalent against the
/// EF Core InMemory provider, so tests exercise TicketService against this fake instead.
/// </summary>
public class FakeTicketReferenceGenerator : ITicketReferenceGenerator
{
    private int _counter;

    public Task<string> GenerateAsync(int year, CancellationToken cancellationToken)
    {
        _counter++;
        return Task.FromResult($"TCK-{year}-{_counter:D4}");
    }
}
