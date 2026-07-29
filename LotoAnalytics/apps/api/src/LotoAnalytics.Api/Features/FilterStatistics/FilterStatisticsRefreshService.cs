using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Features.FilterStatistics;

public interface IFilterStatisticsRefreshService
{
    Task RefreshAsync(string modeCode, CancellationToken cancellationToken);
}

public sealed class FilterStatisticsRefreshService(LotoAnalyticsDbContext dbContext) : IFilterStatisticsRefreshService
{
    // Recalcula e substitui as estatisticas de filtro da modalidade a partir dos concursos salvos.
    public async Task RefreshAsync(string modeCode, CancellationToken cancellationToken)
    {
        // As metricas sao calibradas para o volante 5x5 da Lotofacil.
        if (modeCode != "lotofacil")
        {
            return;
        }

        var draws = await dbContext.Contests
            .AsNoTracking()
            .Where(contest => contest.LotteryMode!.Code == modeCode)
            .OrderBy(contest => contest.Number)
            .Select(contest => contest.Numbers
                .Where(number => number.NumberType == "principal" && number.NumericValue != null)
                .OrderBy(number => number.NumericValue)
                .Select(number => number.NumericValue!.Value)
                .ToArray())
            .ToArrayAsync(cancellationToken);

        var buckets = FilterStatisticsAggregator.Aggregate(draws);
        var now = DateTimeOffset.UtcNow;

        await dbContext.FilterStatistics
            .Where(statistic => statistic.LotteryModeCode == modeCode)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.FilterStatistics.AddRange(buckets.Select(bucket => new FilterStatistic
        {
            LotteryModeCode = modeCode,
            Category = bucket.Category,
            Value = bucket.Value,
            Count = bucket.Count,
            UpdatedAt = now
        }));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
