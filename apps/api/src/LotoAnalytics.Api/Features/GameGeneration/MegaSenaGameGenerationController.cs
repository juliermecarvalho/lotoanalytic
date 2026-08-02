using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LotoAnalytics.Api.Features.Users;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Features.GameGeneration;

[ApiController]
[Route("gerador/mega-sena")]
public sealed class MegaSenaGameGenerationController(IServiceProvider serviceProvider) : ControllerBase
{
    // Gera jogos da Mega-Sena usando os filtros estatisticos informados no corpo da requisicao.
    [AllowAnonymous]
    [HttpPost("gerar")]
    public async Task<ActionResult<MegaSenaGameGenerationResponse>> Generate(
        [FromBody] GenerateMegaSenaGamesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var forbiddenGameKeys = request.OnlyUnseenGames
                ? await LoadDrawnGameKeysAsync(cancellationToken)
                : null;

            var result = MegaSenaGameGenerator.Generate(new MegaSenaGameGenerationRequest
            {
                GameCount = request.GameCount,
                NumbersPerGame = request.NumbersPerGame,
                RequiredNumbers = request.RequiredNumbers,
                ExcludedNumbers = request.ExcludedNumbers,
                PreviousNumbers = request.PreviousNumbers,
                EvenCount = request.EvenCount,
                OddCount = request.OddCount,
                MinimumSum = request.MinimumSum,
                MaximumSum = request.MaximumSum,
                SumRanges = request.SumRanges
                    .Select(range => new SumRangeFilter(range.MinimumSum, range.MaximumSum))
                    .ToArray(),
                MinimumRepeated = request.MinimumRepeated,
                MaximumRepeated = request.MaximumRepeated,
                MinimumPrimes = request.MinimumPrimes,
                MaximumPrimes = request.MaximumPrimes,
                MinimumPerRowColumn = request.MinimumPerRowColumn,
                MaximumPerRowColumn = request.MaximumPerRowColumn,
                MaximumSequence = request.MaximumSequence,
                ForbiddenGameKeys = forbiddenGameKeys
            });

            if (result.Games.Count > 0 && User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await SynchronizeCurrentUserAsync(cancellationToken);
                var historyService = serviceProvider.GetRequiredService<IGameGenerationHistoryService>();
                var summaries = result.Games
                    .Select(game => new GeneratedGameSummary(game.Numbers, game.EvenCount, game.OddCount, game.NumbersSum))
                    .ToArray();
                await historyService.SaveAsync(
                    currentUser.User.Id,
                    "mega_sena",
                    request.NumbersPerGame,
                    request,
                    summaries,
                    cancellationToken);
            }

            return Ok(new MegaSenaGameGenerationResponse(
                Games: result.Games.Select(MapGeneratedGame).ToArray(),
                AttemptCount: result.AttemptCount));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { erro = exception.Message });
        }
    }

    // Sincroniza o usuario autenticado para consultar os limites do plano atual.
    private async Task<CurrentUserSynchronizationResult> SynchronizeCurrentUserAsync(CancellationToken cancellationToken)
    {
        var synchronizer = serviceProvider.GetRequiredService<ICurrentUserSynchronizer>();
        return await synchronizer.SynchronizeAsync(User, cancellationToken);
    }

    // Carrega as chaves dos sorteios ja registrados da Mega-Sena para gerar apenas jogos ineditos.
    private async Task<IReadOnlySet<string>> LoadDrawnGameKeysAsync(CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<LotoAnalyticsDbContext>();
        var drawnNumbers = await dbContext.ContestNumbers
            .AsNoTracking()
            .Where(number => number.NumberType == "principal" &&
                number.Contest!.LotteryMode!.Code == "mega_sena" &&
                number.NumericValue != null)
            .Select(number => new { number.ContestId, number.NumericValue })
            .ToArrayAsync(cancellationToken);

        return drawnNumbers
            .GroupBy(number => number.ContestId)
            .Select(group => MegaSenaGameGenerator.FormatGameKey(group.Select(number => number.NumericValue!.Value).ToArray()))
            .ToHashSet(StringComparer.Ordinal);
    }

    // Converte o jogo gerado para o contrato HTTP em PT-BR.
    private static GeneratedMegaSenaGameResponse MapGeneratedGame(GeneratedMegaSenaGame game)
    {
        return new GeneratedMegaSenaGameResponse(
            Numbers: game.Numbers,
            EvenCount: game.EvenCount,
            OddCount: game.OddCount,
            NumbersSum: game.NumbersSum,
            RepeatedFromPreviousCount: game.RepeatedFromPreviousCount,
            PrimeCount: game.PrimeCount,
            LongestSequence: game.LongestSequence);
    }
}

public sealed record GenerateMegaSenaGamesRequest
{
    [JsonPropertyName("quantidadeJogos")]
    [Range(1, 100)]
    public int GameCount { get; init; }

    [JsonPropertyName("dezenasPorJogo")]
    [Range(6, 20)]
    public int NumbersPerGame { get; init; }

    [JsonPropertyName("dezenasObrigatorias")]
    public IReadOnlyList<string> RequiredNumbers { get; init; } = [];

    [JsonPropertyName("dezenasExcluidas")]
    public IReadOnlyList<string> ExcludedNumbers { get; init; } = [];

    [JsonPropertyName("dezenasAnteriores")]
    public IReadOnlyList<string> PreviousNumbers { get; init; } = [];

    [JsonPropertyName("quantidadePares")]
    [Range(0, 20)]
    public int? EvenCount { get; init; }

    [JsonPropertyName("quantidadeImpares")]
    [Range(0, 20)]
    public int? OddCount { get; init; }

    [JsonPropertyName("somaMinima")]
    [Range(0, 1200)]
    public int? MinimumSum { get; init; }

    [JsonPropertyName("somaMaxima")]
    [Range(0, 1200)]
    public int? MaximumSum { get; init; }

    [JsonPropertyName("faixasSoma")]
    public IReadOnlyList<SumRangeRequest> SumRanges { get; init; } = [];

    [JsonPropertyName("repetidasMinima")]
    [Range(0, 20)]
    public int? MinimumRepeated { get; init; }

    [JsonPropertyName("repetidasMaxima")]
    [Range(0, 20)]
    public int? MaximumRepeated { get; init; }

    [JsonPropertyName("primosMinimo")]
    [Range(0, 17)]
    public int? MinimumPrimes { get; init; }

    [JsonPropertyName("primosMaximo")]
    [Range(0, 17)]
    public int? MaximumPrimes { get; init; }

    [JsonPropertyName("linhaColunaMinima")]
    [Range(0, 20)]
    public int? MinimumPerRowColumn { get; init; }

    [JsonPropertyName("linhaColunaMaxima")]
    [Range(0, 20)]
    public int? MaximumPerRowColumn { get; init; }

    [JsonPropertyName("sequenciaMaxima")]
    [Range(1, 20)]
    public int? MaximumSequence { get; init; }

    [JsonPropertyName("apenasIneditos")]
    public bool OnlyUnseenGames { get; init; }
}

public sealed record MegaSenaGameGenerationResponse(
    [property: JsonPropertyName("jogos")] IReadOnlyList<GeneratedMegaSenaGameResponse> Games,
    [property: JsonPropertyName("combinacoesTestadas")] int AttemptCount);

public sealed record GeneratedMegaSenaGameResponse(
    [property: JsonPropertyName("dezenas")] IReadOnlyList<string> Numbers,
    [property: JsonPropertyName("quantidadePares")] int EvenCount,
    [property: JsonPropertyName("quantidadeImpares")] int OddCount,
    [property: JsonPropertyName("somaDezenas")] int NumbersSum,
    [property: JsonPropertyName("quantidadeRepetidas")] int RepeatedFromPreviousCount,
    [property: JsonPropertyName("quantidadePrimos")] int PrimeCount,
    [property: JsonPropertyName("maiorSequencia")] int LongestSequence);
