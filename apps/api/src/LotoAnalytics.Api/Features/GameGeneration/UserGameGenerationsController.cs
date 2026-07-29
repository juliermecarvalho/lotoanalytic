using System.Text;
using System.Text.Json.Serialization;
using LotoAnalytics.Api.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotoAnalytics.Api.Features.GameGeneration;

[ApiController]
[Route("usuarios/me/geracoes")]
public sealed class UserGameGenerationsController(
    ICurrentUserSynchronizer currentUserSynchronizer,
    IGameGenerationHistoryService historyService)
    : ControllerBase
{
    // Lista o historico de geracoes de jogos do usuario autenticado.
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<GameGenerationHistoryResponse>> List(CancellationToken cancellationToken)
    {
        var currentUser = await currentUserSynchronizer.SynchronizeAsync(User, cancellationToken);
        var history = await historyService.ListByUserAsync(currentUser.User.Id, cancellationToken);

        return Ok(new GameGenerationHistoryResponse(history.Select(MapGeneration).ToArray()));
    }

    // Exporta uma geracao de jogos em CSV quando o plano do usuario permite.
    [Authorize]
    [HttpGet("{generationId:guid}/exportar-csv")]
    public async Task<IActionResult> ExportCsv(Guid generationId, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserSynchronizer.SynchronizeAsync(User, cancellationToken);
        if (!currentUser.Plan.CanExportCsv)
        {
            return Forbid();
        }

        var generation = await historyService.GetByUserAsync(currentUser.User.Id, generationId, cancellationToken);
        if (generation is null)
        {
            return NotFound();
        }

        var csv = BuildCsv(generation);
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"geracao-{generation.Id}.csv");
    }

    // Converte uma geracao persistida para o contrato HTTP em PT-BR.
    private static GameGenerationHistoryItemResponse MapGeneration(GameGenerationHistoryItem generation)
    {
        return new GameGenerationHistoryItemResponse(
            Id: generation.Id,
            GameCount: generation.GameCount,
            NumbersPerGame: generation.NumbersPerGame,
            CreatedAt: generation.CreatedAt,
            Games: generation.Games.Select(MapGame).ToArray());
    }

    // Converte um jogo persistido para o contrato HTTP em PT-BR.
    private static GameGenerationHistoryGameResponse MapGame(GameGenerationHistoryGameItem game)
    {
        return new GameGenerationHistoryGameResponse(
            GameNumber: game.GameNumber,
            Numbers: game.Numbers,
            NumbersSum: game.NumbersSum);
    }

    // Monta o conteudo CSV exportado para automacao externa.
    private static string BuildCsv(GameGenerationHistoryItem generation)
    {
        var builder = new StringBuilder();
        builder.AppendLine("numero_jogo,dezenas,soma_dezenas");

        foreach (var game in generation.Games.OrderBy(game => game.GameNumber))
        {
            builder
                .Append(game.GameNumber)
                .Append(",\"")
                .Append(string.Join(" ", game.Numbers))
                .Append("\",")
                .Append(game.NumbersSum)
                .AppendLine();
        }

        return builder.ToString();
    }
}

public sealed record GameGenerationHistoryResponse(
    [property: JsonPropertyName("geracoes")] IReadOnlyList<GameGenerationHistoryItemResponse> Generations);

public sealed record GameGenerationHistoryItemResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("quantidadeJogos")] int GameCount,
    [property: JsonPropertyName("dezenasPorJogo")] int NumbersPerGame,
    [property: JsonPropertyName("criadoEm")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("jogos")] IReadOnlyList<GameGenerationHistoryGameResponse> Games);

public sealed record GameGenerationHistoryGameResponse(
    [property: JsonPropertyName("numeroJogo")] int GameNumber,
    [property: JsonPropertyName("dezenas")] IReadOnlyList<string> Numbers,
    [property: JsonPropertyName("somaDezenas")] int NumbersSum);
