namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class FilterStatistic
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string LotteryModeCode { get; set; }

    public required string Category { get; set; }

    public int Value { get; set; }

    public int Count { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
