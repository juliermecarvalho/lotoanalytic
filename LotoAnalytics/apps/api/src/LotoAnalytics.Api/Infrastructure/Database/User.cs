namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid KeycloakSubject { get; set; }

    public string? Username { get; set; }

    public string? Email { get; set; }

    public string PlanCode { get; set; } = "gratis";

    public bool Active { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
