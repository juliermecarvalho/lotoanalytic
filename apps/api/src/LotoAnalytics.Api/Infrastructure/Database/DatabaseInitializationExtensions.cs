using LotoAnalytics.Api.Features.FilterStatistics;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Infrastructure.Database;

public static class DatabaseInitializationExtensions
{
    // Aplica migrations pendentes quando a API estiver configurada com banco.
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        if (app.Configuration.GetConnectionString("DefaultConnection") is not { Length: > 0 })
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LotoAnalyticsDbContext>();
        await dbContext.Database.MigrateAsync();

        await BackfillFilterStatisticsAsync(scope.ServiceProvider, dbContext);
    }

    // Preenche as estatisticas de filtro quando ja existem concursos mas a tabela nunca foi calculada.
    private static async Task BackfillFilterStatisticsAsync(IServiceProvider services, LotoAnalyticsDbContext dbContext)
    {
        const string modeCode = "lotofacil";

        var hasStatistics = await dbContext.FilterStatistics
            .AnyAsync(statistic => statistic.LotteryModeCode == modeCode);
        if (hasStatistics)
        {
            return;
        }

        var hasContests = await dbContext.Contests
            .AnyAsync(contest => contest.LotteryMode!.Code == modeCode);
        if (!hasContests)
        {
            return;
        }

        var refreshService = services.GetRequiredService<IFilterStatisticsRefreshService>();
        await refreshService.RefreshAsync(modeCode, CancellationToken.None);
    }
}
