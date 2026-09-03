namespace SupportDesk.Api.Data;

/// <summary>
/// Generates unique, human-readable ticket references such as TCK-2026-0001.
/// Implementations must be safe under concurrent requests and must not rely on an
/// in-memory counter (which would not survive an app restart or a multi-instance deployment).
/// </summary>
public interface ITicketReferenceGenerator
{
    Task<string> GenerateAsync(int year, CancellationToken cancellationToken);
}
