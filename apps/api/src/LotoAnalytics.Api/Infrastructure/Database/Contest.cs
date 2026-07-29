namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class Contest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LotteryModeId { get; set; }

    public int Number { get; set; }

    public int? PreviousContestNumber { get; set; }

    public int? NextContestNumber { get; set; }

    public DateOnly? DrawDate { get; set; }

    public DateOnly? NextContestDate { get; set; }

    public string? DrawLocation { get; set; }

    public string? DrawCityState { get; set; }

    public bool Accumulated { get; set; }

    public bool LatestContest { get; set; }

    public decimal? CollectedAmount { get; set; }

    public decimal? NextContestEstimatedValue { get; set; }

    public decimal? NextContestAccumulatedValue { get; set; }

    public string? SpecialResult { get; set; }

    public required string RawResultJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public LotteryMode? LotteryMode { get; set; }

    public List<ContestNumber> Numbers { get; set; } = [];

    public List<ContestPrizeTier> PrizeTiers { get; set; } = [];

    public List<ContestWinnerCity> WinnerCities { get; set; } = [];
}
