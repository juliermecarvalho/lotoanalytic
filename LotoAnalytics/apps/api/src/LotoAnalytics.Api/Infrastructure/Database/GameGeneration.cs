namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class GameGeneration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string LotteryModeCode { get; set; }

    public int GameCount { get; set; }

    public int NumbersPerGame { get; set; }

    public required string FiltersJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<GeneratedGame> Games { get; set; } = [];
}
