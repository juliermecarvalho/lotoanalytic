using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LotoAnalytics.Api.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotoAnalytics.Api.Features.GameChecking;

[ApiController]
[Route("conferencias/lotofacil")]
public sealed class LotofacilGameCheckingController(IServiceProvider serviceProvider) : ControllerBase
{
    // Confere jogos informados pelo usuario contra um resultado oficial da Lotofacil.
    [Authorize]
    [HttpPost("conferir")]
    public async Task<ActionResult<LotofacilGameCheckingResponse>> Check(
        [FromBody] CheckLotofacilGamesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await SynchronizeCurrentUserAsync(cancellationToken);
            var result = LotofacilGameChecker.Check(request.DrawnNumbers, request.Games);
            var historyService = serviceProvider.GetRequiredService<IGameCheckingHistoryService>();

            await historyService.SaveAsync(currentUser.User.Id, request, result, cancellationToken);

            return Ok(new LotofacilGameCheckingResponse(
                Games: result.Games.Select(MapCheckedGame).ToArray(),
                AwardSummary: result.AwardSummary));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { erro = exception.Message });
        }
    }

    // Sincroniza o usuario autenticado para vincular o historico de conferencia.
    private async Task<CurrentUserSynchronizationResult> SynchronizeCurrentUserAsync(CancellationToken cancellationToken)
    {
        var synchronizer = serviceProvider.GetRequiredService<ICurrentUserSynchronizer>();
        return await synchronizer.SynchronizeAsync(User, cancellationToken);
    }

    // Converte o resultado interno para o contrato HTTP em PT-BR.
    private static CheckedGameResponse MapCheckedGame(LotofacilCheckedGame game)
    {
        return new CheckedGameResponse(
            GameNumber: game.GameIndex,
            HitCount: game.HitCount,
            MatchedNumbers: game.MatchedNumbers,
            IsAwarded: game.IsAwarded);
    }
}

public sealed record CheckLotofacilGamesRequest
{
    [JsonPropertyName("dezenasSorteadas")]
    [Required]
    [MinLength(15)]
    [MaxLength(15)]
    public required IReadOnlyList<string> DrawnNumbers { get; init; }

    [JsonPropertyName("jogos")]
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<IReadOnlyList<string>> Games { get; init; }
}

public sealed record LotofacilGameCheckingResponse(
    [property: JsonPropertyName("jogos")] IReadOnlyList<CheckedGameResponse> Games,
    [property: JsonPropertyName("resumoPremiacao")] IReadOnlyDictionary<int, int> AwardSummary);

public sealed record CheckedGameResponse(
    [property: JsonPropertyName("numeroJogo")] int GameNumber,
    [property: JsonPropertyName("quantidadeAcertos")] int HitCount,
    [property: JsonPropertyName("dezenasAcertadas")] IReadOnlyList<string> MatchedNumbers,
    [property: JsonPropertyName("premiado")] bool IsAwarded);
