using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LotoAnalytics.Api.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotoAnalytics.Api.Features.GameGeneration;

[ApiController]
[Route("gerador/lotomania")]
public sealed class LotomaniaGameGenerationController(IServiceProvider serviceProvider) : ControllerBase
{
    // Gera apostas da Lotomania usando os filtros estatisticos informados no corpo da requisicao.
    // A Lotomania nao tem "apenas ineditos": o sorteio traz 20 dezenas e a aposta tem 50, entao nao ha
    // como uma aposta coincidir com um sorteio ja registrado.
    [AllowAnonymous]
    [HttpPost("gerar")]
    public async Task<ActionResult<LotomaniaGameGenerationResponse>> Generate(
        [FromBody] GenerateLotomaniaGamesRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = LotomaniaGameGenerator.Generate(new LotomaniaGameGenerationRequest
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
                MaximumSequence = request.MaximumSequence
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
                    "lotomania",
                    request.NumbersPerGame,
                    request,
                    summaries,
                    cancellationToken);
            }

            return Ok(new LotomaniaGameGenerationResponse(
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

    // Converte a aposta gerada para o contrato HTTP em PT-BR.
    private static GeneratedLotomaniaGameResponse MapGeneratedGame(GeneratedLotomaniaGame game)
    {
        return new GeneratedLotomaniaGameResponse(
            Numbers: game.Numbers,
            EvenCount: game.EvenCount,
            OddCount: game.OddCount,
            NumbersSum: game.NumbersSum,
            RepeatedFromPreviousCount: game.RepeatedFromPreviousCount,
            PrimeCount: game.PrimeCount,
            LongestSequence: game.LongestSequence);
    }
}

public sealed record GenerateLotomaniaGamesRequest
{
    [JsonPropertyName("quantidadeJogos")]
    [Range(1, 100)]
    public int GameCount { get; init; }

    [JsonPropertyName("dezenasPorJogo")]
    [Range(50, 50)]
    public int NumbersPerGame { get; init; } = 50;

    [JsonPropertyName("dezenasObrigatorias")]
    public IReadOnlyList<string> RequiredNumbers { get; init; } = [];

    [JsonPropertyName("dezenasExcluidas")]
    public IReadOnlyList<string> ExcludedNumbers { get; init; } = [];

    [JsonPropertyName("dezenasAnteriores")]
    public IReadOnlyList<string> PreviousNumbers { get; init; } = [];

    [JsonPropertyName("quantidadePares")]
    [Range(0, 50)]
    public int? EvenCount { get; init; }

    [JsonPropertyName("quantidadeImpares")]
    [Range(0, 50)]
    public int? OddCount { get; init; }

    [JsonPropertyName("somaMinima")]
    [Range(0, 3800)]
    public int? MinimumSum { get; init; }

    [JsonPropertyName("somaMaxima")]
    [Range(0, 3800)]
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
    [Range(0, 25)]
    public int? MinimumPrimes { get; init; }

    [JsonPropertyName("primosMaximo")]
    [Range(0, 25)]
    public int? MaximumPrimes { get; init; }

    [JsonPropertyName("linhaColunaMinima")]
    [Range(0, 50)]
    public int? MinimumPerRowColumn { get; init; }

    [JsonPropertyName("linhaColunaMaxima")]
    [Range(0, 50)]
    public int? MaximumPerRowColumn { get; init; }

    [JsonPropertyName("sequenciaMaxima")]
    [Range(1, 50)]
    public int? MaximumSequence { get; init; }
}

public sealed record LotomaniaGameGenerationResponse(
    [property: JsonPropertyName("jogos")] IReadOnlyList<GeneratedLotomaniaGameResponse> Games,
    [property: JsonPropertyName("combinacoesTestadas")] int AttemptCount);

public sealed record GeneratedLotomaniaGameResponse(
    [property: JsonPropertyName("dezenas")] IReadOnlyList<string> Numbers,
    [property: JsonPropertyName("quantidadePares")] int EvenCount,
    [property: JsonPropertyName("quantidadeImpares")] int OddCount,
    [property: JsonPropertyName("somaDezenas")] int NumbersSum,
    [property: JsonPropertyName("quantidadeRepetidas")] int RepeatedFromPreviousCount,
    [property: JsonPropertyName("quantidadePrimos")] int PrimeCount,
    [property: JsonPropertyName("maiorSequencia")] int LongestSequence);
