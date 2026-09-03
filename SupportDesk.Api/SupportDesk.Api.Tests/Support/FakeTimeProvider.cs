namespace SupportDesk.Api.Tests.Support;

/// <summary>Controllable clock so tests can assert on exact server-generated timestamps.</summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Set(DateTimeOffset utcNow) => _utcNow = utcNow;
}
