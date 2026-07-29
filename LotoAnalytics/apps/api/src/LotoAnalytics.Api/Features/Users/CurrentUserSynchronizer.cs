using System.Security.Claims;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Features.Users;

public interface ICurrentUserSynchronizer
{
    Task<CurrentUserSynchronizationResult> SynchronizeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}

public sealed class CurrentUserSynchronizer(LotoAnalyticsDbContext dbContext) : ICurrentUserSynchronizer
{
    // Sincroniza o usuario autenticado pelo Keycloak na tabela usuarios.
    public async Task<CurrentUserSynchronizationResult> SynchronizeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var subject = ReadSubject(principal);
        var now = DateTimeOffset.UtcNow;
        var user = await dbContext.Users
            .SingleOrDefaultAsync(existingUser => existingUser.KeycloakSubject == subject, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                KeycloakSubject = subject,
                CreatedAt = now
            };
            dbContext.Users.Add(user);
        }

        user.Username = principal.FindFirstValue("preferred_username") ?? principal.Identity?.Name;
        user.Email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
        user.PlanCode = ResolvePlanCode(principal);
        user.LastLoginAt = now;
        user.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        var plan = await dbContext.Plans
            .SingleAsync(existingPlan => existingPlan.Code == user.PlanCode, cancellationToken);

        return new CurrentUserSynchronizationResult(user, plan);
    }

    // Le e valida o identificador estavel do usuario emitido pelo Keycloak.
    private static Guid ReadSubject(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var parsedSubject))
        {
            throw new InvalidOperationException("O token autenticado nao possui um subject Keycloak valido.");
        }

        return parsedSubject;
    }

    // Resolve o plano da aplicacao a partir dos papeis emitidos pelo Keycloak.
    private static string ResolvePlanCode(ClaimsPrincipal principal)
    {
        if (principal.IsInRole("administrador") || principal.IsInRole("usuario_premium"))
        {
            return "premium";
        }

        return "gratis";
    }
}

public sealed record CurrentUserSynchronizationResult(User User, Plan Plan);
