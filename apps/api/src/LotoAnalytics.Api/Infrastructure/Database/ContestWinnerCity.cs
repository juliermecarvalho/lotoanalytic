namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class ContestWinnerCity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContestId { get; set; }

    public required string City { get; set; }

    public required string State { get; set; }

    public int WinnersCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Contest? Contest { get; set; }
}
