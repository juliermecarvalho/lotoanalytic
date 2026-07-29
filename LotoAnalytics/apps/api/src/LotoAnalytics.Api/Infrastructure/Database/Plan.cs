namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class Plan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Code { get; set; }

    public required string Name { get; set; }

    public int GameGenerationLimit { get; set; }

    public bool CanExportCsv { get; set; }

    public bool CanExportPdf { get; set; }

    public bool Active { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
