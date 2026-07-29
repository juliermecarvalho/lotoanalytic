using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotoAnalytics.Api.Features.Users;

[ApiController]
[Route("usuarios")]
public sealed class CurrentUserController(IServiceProvider serviceProvider) : ControllerBase
{
    // Retorna os dados basicos do usuario autenticado pelo Keycloak.
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Unauthorized();
        }

        var synchronizer = serviceProvider.GetService<ICurrentUserSynchronizer>();
        var synchronizedUserResult = synchronizer is null
            ? null
            : await synchronizer.SynchronizeAsync(User, cancellationToken);
        var synchronizedUser = synchronizedUserResult?.User;

        return Ok(new CurrentUserResponse(
            Id: synchronizedUser?.Id ?? Guid.Empty,
            Subject: subject,
            Username: User.FindFirstValue("preferred_username") ?? User.Identity?.Name,
            Email: User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email"),
            Roles: User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct(StringComparer.Ordinal).Order().ToArray(),
            LastLoginAt: synchronizedUser?.LastLoginAt,
            CurrentPlan: synchronizedUserResult is null ? null : MapPlan(synchronizedUserResult.Plan)));
    }

    // Converte o plano persistido para o contrato HTTP em PT-BR.
    private static CurrentPlanResponse MapPlan(Infrastructure.Database.Plan plan)
    {
        return new CurrentPlanResponse(
            Code: plan.Code,
            Name: plan.Name,
            GameGenerationLimit: plan.GameGenerationLimit,
            CanExportCsv: plan.CanExportCsv,
            CanExportPdf: plan.CanExportPdf);
    }
}

public sealed record CurrentUserResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles,
    [property: JsonPropertyName("ultimoLoginEm")] DateTimeOffset? LastLoginAt,
    [property: JsonPropertyName("planoAtual")] CurrentPlanResponse? CurrentPlan);

public sealed record CurrentPlanResponse(
    [property: JsonPropertyName("codigo")] string Code,
    [property: JsonPropertyName("nome")] string Name,
    [property: JsonPropertyName("limiteJogosPorGeracao")] int GameGenerationLimit,
    [property: JsonPropertyName("permiteExportarCsv")] bool CanExportCsv,
    [property: JsonPropertyName("permiteExportarPdf")] bool CanExportPdf);
