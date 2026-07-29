using System.Text.Json.Serialization;
using LotoAnalytics.Api.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotoAnalytics.Api.Features.GameChecking;

[ApiController]
[Route("usuarios/me/conferencias")]
public sealed class UserGameCheckingsController(
    ICurrentUserSynchronizer currentUserSynchronizer,
    IGameCheckingHistoryService historyService)
    : ControllerBase
{
    // Lista o historico de conferencias do usuario autenticado.
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<GameCheckingHistoryResponse>> List(CancellationToken cancellationToken)
    {
        var currentUser = await currentUserSynchronizer.SynchronizeAsync(User, cancellationToken);
        var history = await historyService.ListByUserAsync(currentUser.User.Id, cancellationToken);

        return Ok(new GameCheckingHistoryResponse(history.Select(MapChecking).ToArray()));
    }

    // Converte uma conferencia persistida para o contrato HTTP em PT-BR.
    private static GameCheckingHistoryItemResponse MapChecking(GameCheckingHistoryItem checking)
    {
        return new GameCheckingHistoryItemResponse(
            Id: checking.Id,
            GameCount: checking.GameCount,
            CreatedAt: checking.CreatedAt,
            AwardSummary: checking.AwardSummary,
            Games: checking.Games.Select(MapGame).ToArray());
    }

    // Converte um jogo conferido para o contrato HTTP em PT-BR.
    private static GameCheckingHistoryGameResponse MapGame(GameCheckingHistoryGameItem game)
    {
        return new GameCheckingHistoryGameResponse(
            GameNumber: game.GameNumber,
            HitCount: game.HitCount,
            MatchedNumbers: game.MatchedNumbers);
    }
}

public sealed record GameCheckingHistoryResponse(
    [property: JsonPropertyName("conferencias")] IReadOnlyList<GameCheckingHistoryItemResponse> Checkings);

public sealed record GameCheckingHistoryItemResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("quantidadeJogos")] int GameCount,
    [property: JsonPropertyName("criadoEm")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("resumoPremiacao")] IReadOnlyDictionary<int, int> AwardSummary,
    [property: JsonPropertyName("jogos")] IReadOnlyList<GameCheckingHistoryGameResponse> Games);

public sealed record GameCheckingHistoryGameResponse(
    [property: JsonPropertyName("numeroJogo")] int GameNumber,
    [property: JsonPropertyName("quantidadeAcertos")] int HitCount,
    [property: JsonPropertyName("dezenasAcertadas")] IReadOnlyList<string> MatchedNumbers);
