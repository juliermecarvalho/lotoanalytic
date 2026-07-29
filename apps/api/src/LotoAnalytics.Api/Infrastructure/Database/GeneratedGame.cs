namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class GeneratedGame
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid GameGenerationId { get; set; }

    public GameGeneration GameGeneration { get; set; } = null!;

    public int GameNumber { get; set; }

    public string[] Numbers { get; set; } = [];

    public int EvenCount { get; set; }

    public int OddCount { get; set; }

    public int NumbersSum { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
