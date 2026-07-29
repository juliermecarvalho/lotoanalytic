namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class LotteryMode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string CaixaGameType { get; set; }

    public int? CaixaGameNumber { get; set; }

    public int MainNumbersCount { get; set; }

    public decimal? SimpleBetPrice { get; set; }

    public int? SecondDrawNumbersCount { get; set; }

    public bool HasTrevos { get; set; }

    public bool HasHeartTeam { get; set; }

    public bool HasLuckyMonth { get; set; }

    public bool Active { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
