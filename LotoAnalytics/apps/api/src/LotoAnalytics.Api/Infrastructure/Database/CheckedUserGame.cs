namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class CheckedUserGame
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid GameCheckingId { get; set; }

    public GameChecking GameChecking { get; set; } = null!;

    public int GameNumber { get; set; }

    public string[] Numbers { get; set; } = [];

    public int HitCount { get; set; }

    public string[] MatchedNumbers { get; set; } = [];

    public bool Awarded { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
