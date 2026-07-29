namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class ContestPrizeTier
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContestId { get; set; }

    public int Tier { get; set; }

    public required string Description { get; set; }

    public int WinnersCount { get; set; }

    public decimal PrizeValue { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Contest? Contest { get; set; }
}
