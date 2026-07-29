namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class GameChecking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string LotteryModeCode { get; set; }

    public string[] DrawnNumbers { get; set; } = [];

    public int GameCount { get; set; }

    public required string AwardSummaryJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<CheckedUserGame> Games { get; set; } = [];
}
