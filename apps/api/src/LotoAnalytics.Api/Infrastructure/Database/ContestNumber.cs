namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class ContestNumber
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContestId { get; set; }

    public required string NumberType { get; set; }

    public int Position { get; set; }

    public required string Value { get; set; }

    public int? NumericValue { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Contest? Contest { get; set; }
}
