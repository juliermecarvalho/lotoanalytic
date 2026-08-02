using System.Text.Json.Serialization;
using LotoAnalytics.Api.Features.GameGeneration;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Features.Dashboard;

[ApiController]
[Route("estatisticas")]
public sealed class LotofacilDashboardController(LotoAnalyticsDbContext dbContext) : ControllerBase
{
    // Configuracao de painel por modalidade suportada; demais modalidades usam o volante da Lotofacil.
    private static readonly IReadOnlyDictionary<string, DashboardBoardConfig> BoardConfigs =
        new Dictionary<string, DashboardBoardConfig>
        {
            ["lotofacil"] = LotofacilDashboardAggregator.LotofacilConfig,
            ["mega_sena"] = new()
            {
                Board = MegaSenaGameGenerator.Board,
                PreferredSumLowerBound = 150,
                PreferredSumUpperBound = 210,
                IncludeGrid = false
            },
            ["quina"] = new()
            {
                Board = QuinaGameGenerator.Board,
                PreferredSumLowerBound = 165,
                PreferredSumUpperBound = 240,
                IncludeGrid = false
            },
            ["lotomania"] = new()
            {
                // Sorteio da Lotomania traz 20 dezenas de 00 a 99; a soma media fica em torno de 990.
                Board = LotomaniaGameGenerator.Board,
                PreferredSumLowerBound = 880,
                PreferredSumUpperBound = 1100,
                IncludeGrid = false
            },
            ["dupla_sena"] = new()
            {
                // Sorteio da Dupla Sena traz 6 dezenas de 01 a 50; a soma media fica em torno de 153.
                Board = DuplaSenaGameGenerator.Board,
                PreferredSumLowerBound = 120,
                PreferredSumUpperBound = 190,
                IncludeGrid = false
            },
            ["maismilionaria"] = new()
            {
                // Sorteio da +Milionaria traz 6 dezenas de 01 a 50; a soma media fica em torno de 153.
                Board = MaisMilionariaGameGenerator.Board,
                PreferredSumLowerBound = 120,
                PreferredSumUpperBound = 190,
                IncludeGrid = false
            }
        };

    // Monta o painel estatistico consolidado da modalidade a partir dos concursos salvos.
    [HttpGet("{codigoModalidade}/painel")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(
        string codigoModalidade,
        CancellationToken cancellationToken)
    {
        var draws = await dbContext.Contests
            .AsNoTracking()
            .Where(contest => contest.LotteryMode!.Code == codigoModalidade)
            .OrderBy(contest => contest.Number)
            .Select(contest => new
            {
                contest.Number,
                contest.DrawDate,
                Numbers = contest.Numbers
                    .Where(number => number.NumberType == "principal" && number.NumericValue != null)
                    .OrderBy(number => number.NumericValue)
                    .Select(number => number.NumericValue!.Value)
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);

        var orderedDraws = draws
            .Select(draw => new DashboardDraw(draw.Number, draw.DrawDate, draw.Numbers))
            .ToArray();

        var config = BoardConfigs.GetValueOrDefault(codigoModalidade, LotofacilDashboardAggregator.LotofacilConfig);
        var snapshot = LotofacilDashboardAggregator.Aggregate(orderedDraws, config);

        var trevos = await LoadTrevoFrequenciesAsync(codigoModalidade, cancellationToken);

        return Ok(MapToResponse(codigoModalidade, snapshot, trevos));
    }

    // Calcula a frequencia dos trevos (01-06) para modalidades que os sorteiam (ex.: +Milionaria).
    // Retorna null quando a modalidade nao tem trevos registrados, mantendo o painel das demais intacto.
    private async Task<IReadOnlyList<DashboardTrevoFrequencyResponse>?> LoadTrevoFrequenciesAsync(
        string codigoModalidade,
        CancellationToken cancellationToken)
    {
        var trevoDraws = await dbContext.Contests
            .AsNoTracking()
            .Where(contest => contest.LotteryMode!.Code == codigoModalidade)
            .Where(contest => contest.Numbers.Any(number => number.NumberType == "trevo" && number.NumericValue != null))
            .OrderBy(contest => contest.Number)
            .Select(contest => new
            {
                contest.Number,
                Trevos = contest.Numbers
                    .Where(number => number.NumberType == "trevo" && number.NumericValue != null)
                    .Select(number => number.NumericValue!.Value)
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);

        if (trevoDraws.Length == 0)
        {
            return null;
        }

        var totalDraws = trevoDraws.Length;
        var latestContest = trevoDraws[^1].Number;

        return Enumerable
            .Range(1, 6)
            .Select(trevo =>
            {
                var appearances = trevoDraws.Where(draw => draw.Trevos.Contains(trevo)).ToArray();
                var count = appearances.Length;
                int? lastContest = appearances.Length > 0 ? appearances[^1].Number : null;
                // Atraso: quantos concursos com trevos ja ocorreram desde a ultima aparicao do trevo.
                var delay = lastContest is null
                    ? totalDraws
                    : trevoDraws.Count(draw => draw.Number > lastContest.Value);
                return new DashboardTrevoFrequencyResponse(
                    trevo,
                    count,
                    totalDraws > 0 ? (double)count / totalDraws * 100 : 0,
                    delay,
                    lastContest);
            })
            .ToArray();
    }

    // Traduz o snapshot calculado para o contrato JSON exposto ao frontend.
    private static DashboardResponse MapToResponse(
        string modeCode,
        DashboardSnapshot snapshot,
        IReadOnlyList<DashboardTrevoFrequencyResponse>? trevos)
    {
        var latest = snapshot.LatestContest is null
            ? null
            : new DashboardLatestContestResponse(
                snapshot.LatestContest.ContestNumber,
                snapshot.LatestContest.DrawDate,
                snapshot.LatestContest.Numbers,
                snapshot.LatestContest.EvenCount,
                snapshot.LatestContest.OddCount,
                snapshot.LatestContest.Sum,
                snapshot.LatestContest.PrimeCount,
                snapshot.LatestContest.BorderCount,
                snapshot.LatestContest.RepeatedFromPrevious);

        var frequencies = snapshot.Frequencies
            .Select(item => new DashboardFrequencyResponse(item.Number, item.Count, item.Percentage, item.Delay, item.LastContest))
            .ToArray();

        var categories = snapshot.Categories.ToDictionary(
            group => group.Key,
            group => (IReadOnlyList<DashboardCategoryItemResponse>)group.Value
                .Select(item => new DashboardCategoryItemResponse(item.Value, item.Count))
                .ToArray());

        return new DashboardResponse(
            ModeCode: modeCode,
            TotalContests: snapshot.TotalContests,
            LatestContest: latest,
            Summary: new DashboardSummaryResponse(
                snapshot.Summary.AverageSum,
                snapshot.Summary.AverageRepetition,
                snapshot.Summary.UniqueCombinationsPercentage,
                snapshot.Summary.PreferredSumPercentage),
            Frequencies: frequencies,
            Categories: categories,
            Trevos: trevos);
    }
}

public sealed record DashboardResponse(
    [property: JsonPropertyName("codigoModalidade")] string ModeCode,
    [property: JsonPropertyName("totalConcursos")] int TotalContests,
    [property: JsonPropertyName("ultimoConcurso")] DashboardLatestContestResponse? LatestContest,
    [property: JsonPropertyName("resumo")] DashboardSummaryResponse Summary,
    [property: JsonPropertyName("frequencias")] IReadOnlyList<DashboardFrequencyResponse> Frequencies,
    [property: JsonPropertyName("categorias")] IReadOnlyDictionary<string, IReadOnlyList<DashboardCategoryItemResponse>> Categories,
    [property: JsonPropertyName("trevos")] IReadOnlyList<DashboardTrevoFrequencyResponse>? Trevos = null);

public sealed record DashboardTrevoFrequencyResponse(
    [property: JsonPropertyName("trevo")] int Trevo,
    [property: JsonPropertyName("quantidade")] int Count,
    [property: JsonPropertyName("percentual")] double Percentage,
    [property: JsonPropertyName("atraso")] int Delay,
    [property: JsonPropertyName("ultimoConcurso")] int? LastContest);

public sealed record DashboardSummaryResponse(
    [property: JsonPropertyName("somaMedia")] double AverageSum,
    [property: JsonPropertyName("repeticaoMedia")] double AverageRepetition,
    [property: JsonPropertyName("combinacoesIneditasPercentual")] double UniqueCombinationsPercentage,
    [property: JsonPropertyName("faixaSomaPreferencialPercentual")] double PreferredSumPercentage);

public sealed record DashboardFrequencyResponse(
    [property: JsonPropertyName("dezena")] int Number,
    [property: JsonPropertyName("quantidade")] int Count,
    [property: JsonPropertyName("percentual")] double Percentage,
    [property: JsonPropertyName("atraso")] int Delay,
    [property: JsonPropertyName("ultimoConcurso")] int? LastContest);

public sealed record DashboardLatestContestResponse(
    [property: JsonPropertyName("numero")] int ContestNumber,
    [property: JsonPropertyName("dataApuracao")] DateOnly? DrawDate,
    [property: JsonPropertyName("dezenas")] IReadOnlyList<string> Numbers,
    [property: JsonPropertyName("pares")] int EvenCount,
    [property: JsonPropertyName("impares")] int OddCount,
    [property: JsonPropertyName("soma")] int Sum,
    [property: JsonPropertyName("primos")] int PrimeCount,
    [property: JsonPropertyName("moldura")] int BorderCount,
    [property: JsonPropertyName("repetidasAnterior")] int RepeatedFromPrevious);

public sealed record DashboardCategoryItemResponse(
    [property: JsonPropertyName("valor")] int Value,
    [property: JsonPropertyName("quantidade")] int Count);
