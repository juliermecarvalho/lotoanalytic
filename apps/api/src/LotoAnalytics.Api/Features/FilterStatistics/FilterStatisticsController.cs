using System.Text.Json.Serialization;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Features.FilterStatistics;

[ApiController]
[Route("estatisticas")]
public sealed class FilterStatisticsController(LotoAnalyticsDbContext dbContext) : ControllerBase
{
    // Consulta as distribuicoes pre-calculadas das estatisticas de filtro de uma modalidade.
    [HttpGet("{codigoModalidade}/filtros")]
    public async Task<ActionResult<FilterStatisticsResponse>> GetFilterStatistics(
        string codigoModalidade,
        CancellationToken cancellationToken)
    {
        var statistics = await dbContext.FilterStatistics
            .AsNoTracking()
            .Where(statistic => statistic.LotteryModeCode == codigoModalidade)
            .OrderBy(statistic => statistic.Category)
            .ThenBy(statistic => statistic.Value)
            .ToArrayAsync(cancellationToken);

        var categories = statistics
            .GroupBy(statistic => statistic.Category)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<FilterStatisticsItemResponse>)group
                    .Select(statistic => new FilterStatisticsItemResponse(statistic.Value, statistic.Count))
                    .ToArray());

        var totalContests = statistics
            .Where(statistic => statistic.Category == FilterStatisticsAggregator.ParityCategory)
            .Sum(statistic => statistic.Count);

        return Ok(new FilterStatisticsResponse(
            ModeCode: codigoModalidade,
            TotalContests: totalContests,
            UpdatedAt: statistics.Length > 0 ? statistics.Max(statistic => statistic.UpdatedAt) : null,
            Categories: categories));
    }
}

public sealed record FilterStatisticsResponse(
    [property: JsonPropertyName("codigoModalidade")] string ModeCode,
    [property: JsonPropertyName("totalConcursos")] int TotalContests,
    [property: JsonPropertyName("atualizadoEm")] DateTimeOffset? UpdatedAt,
    [property: JsonPropertyName("categorias")] IReadOnlyDictionary<string, IReadOnlyList<FilterStatisticsItemResponse>> Categories);

public sealed record FilterStatisticsItemResponse(
    [property: JsonPropertyName("valor")] int Value,
    [property: JsonPropertyName("quantidade")] int Count);
