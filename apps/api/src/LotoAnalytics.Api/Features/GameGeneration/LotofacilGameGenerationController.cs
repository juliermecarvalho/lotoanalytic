using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LotoAnalytics.Api.Features.Users;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Features.GameGeneration;

[ApiController]
[Route("gerador/lotofacil")]
public sealed class LotofacilGameGenerationController(IServiceProvider serviceProvider) : ControllerBase
{
    // Gera jogos da Lotofacil usando os filtros estatisticos informados no corpo da requisicao.
    [AllowAnonymous]
    [HttpPost("gerar")]
    public async Task<ActionResult<LotofacilGameGenerationResponse>> Generate(
        [FromBody] GenerateLotofacilGamesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var forbiddenGameKeys = request.OnlyUnseenGames
                ? await LoadDrawnGameKeysAsync(cancellationToken)
                : null;

            var result = LotofacilGameGenerator.Generate(new LotofacilGameGenerationRequest
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
                MinimumBorder = request.MinimumBorder,
                MaximumBorder = request.MaximumBorder,
                MinimumPerRowColumn = request.MinimumPerRowColumn,
                MaximumPerRowColumn = request.MaximumPerRowColumn,
                MaximumSequence = request.MaximumSequence,
                ForbiddenGameKeys = forbiddenGameKeys
            });

            if (result.Games.Count > 0 && User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await SynchronizeCurrentUserAsync(cancellationToken);
                var historyService = serviceProvider.GetRequiredService<IGameGenerationHistoryService>();
                await historyService.SaveAsync(currentUser.User.Id, request, result, cancellationToken);
            }

            return Ok(new LotofacilGameGenerationResponse(
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

    // Carrega as chaves dos sorteios ja registrados da Lotofacil para gerar apenas jogos ineditos.
    private async Task<IReadOnlySet<string>> LoadDrawnGameKeysAsync(CancellationToken cancellationToken)
    {
        var dbContext = serviceProvider.GetRequiredService<LotoAnalyticsDbContext>();
        var drawnNumbers = await dbContext.ContestNumbers
            .AsNoTracking()
            .Where(number => number.NumberType == "principal" &&
                number.Contest!.LotteryMode!.Code == "lotofacil" &&
                number.NumericValue != null)
            .Select(number => new { number.ContestId, number.NumericValue })
            .ToArrayAsync(cancellationToken);

        return drawnNumbers
            .GroupBy(number => number.ContestId)
            .Select(group => LotofacilGameGenerator.FormatGameKey(group.Select(number => number.NumericValue!.Value).ToArray()))
            .ToHashSet(StringComparer.Ordinal);
    }

    // Converte o jogo gerado para o contrato HTTP em PT-BR.
    private static GeneratedGameResponse MapGeneratedGame(GeneratedLotofacilGame game)
    {
        return new GeneratedGameResponse(
            Numbers: game.Numbers,
            EvenCount: game.EvenCount,
            OddCount: game.OddCount,
            NumbersSum: game.NumbersSum,
            RepeatedFromPreviousCount: game.RepeatedFromPreviousCount,
            PrimeCount: game.PrimeCount,
            BorderCount: game.BorderCount,
            LongestSequence: game.LongestSequence);
    }
}

public sealed record GenerateLotofacilGamesRequest
{
    [JsonPropertyName("quantidadeJogos")]
    [Range(1, 100)]
    public int GameCount { get; init; }

    [JsonPropertyName("dezenasPorJogo")]
    [Range(15, 16)]
    public int NumbersPerGame { get; init; }

    [JsonPropertyName("dezenasObrigatorias")]
    public IReadOnlyList<string> RequiredNumbers { get; init; } = [];

    [JsonPropertyName("dezenasExcluidas")]
    public IReadOnlyList<string> ExcludedNumbers { get; init; } = [];

    [JsonPropertyName("dezenasAnteriores")]
    public IReadOnlyList<string> PreviousNumbers { get; init; } = [];

    [JsonPropertyName("quantidadePares")]
    [Range(0, 16)]
    public int? EvenCount { get; init; }

    [JsonPropertyName("quantidadeImpares")]
    [Range(0, 16)]
    public int? OddCount { get; init; }

    [JsonPropertyName("somaMinima")]
    [Range(0, 400)]
    public int? MinimumSum { get; init; }

    [JsonPropertyName("somaMaxima")]
    [Range(0, 400)]
    public int? MaximumSum { get; init; }

    [JsonPropertyName("faixasSoma")]
    public IReadOnlyList<SumRangeRequest> SumRanges { get; init; } = [];

    [JsonPropertyName("repetidasMinima")]
    [Range(0, 16)]
    public int? MinimumRepeated { get; init; }

    [JsonPropertyName("repetidasMaxima")]
    [Range(0, 16)]
    public int? MaximumRepeated { get; init; }

    [JsonPropertyName("primosMinimo")]
    [Range(0, 9)]
    public int? MinimumPrimes { get; init; }

    [JsonPropertyName("primosMaximo")]
    [Range(0, 9)]
    public int? MaximumPrimes { get; init; }

    [JsonPropertyName("molduraMinima")]
    [Range(0, 16)]
    public int? MinimumBorder { get; init; }

    [JsonPropertyName("molduraMaxima")]
    [Range(0, 16)]
    public int? MaximumBorder { get; init; }

    [JsonPropertyName("linhaColunaMinima")]
    [Range(0, 5)]
    public int? MinimumPerRowColumn { get; init; }

    [JsonPropertyName("linhaColunaMaxima")]
    [Range(0, 5)]
    public int? MaximumPerRowColumn { get; init; }

    [JsonPropertyName("sequenciaMaxima")]
    [Range(1, 16)]
    public int? MaximumSequence { get; init; }

    [JsonPropertyName("apenasIneditos")]
    public bool OnlyUnseenGames { get; init; }
}

public sealed record SumRangeRequest(
    [property: JsonPropertyName("somaMinima")] int MinimumSum,
    [property: JsonPropertyName("somaMaxima")] int MaximumSum);

public sealed record LotofacilGameGenerationResponse(
    [property: JsonPropertyName("jogos")] IReadOnlyList<GeneratedGameResponse> Games,
    [property: JsonPropertyName("combinacoesTestadas")] int AttemptCount);

public sealed record GeneratedGameResponse(
    [property: JsonPropertyName("dezenas")] IReadOnlyList<string> Numbers,
    [property: JsonPropertyName("quantidadePares")] int EvenCount,
    [property: JsonPropertyName("quantidadeImpares")] int OddCount,
    [property: JsonPropertyName("somaDezenas")] int NumbersSum,
    [property: JsonPropertyName("quantidadeRepetidas")] int RepeatedFromPreviousCount,
    [property: JsonPropertyName("quantidadePrimos")] int PrimeCount,
    [property: JsonPropertyName("quantidadeMoldura")] int BorderCount,
    [property: JsonPropertyName("maiorSequencia")] int LongestSequence);
