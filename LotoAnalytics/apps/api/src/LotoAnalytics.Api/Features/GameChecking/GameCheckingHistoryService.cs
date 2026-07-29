using System.Text.Json;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using DbGameChecking = LotoAnalytics.Api.Infrastructure.Database.GameChecking;

namespace LotoAnalytics.Api.Features.GameChecking;

public interface IGameCheckingHistoryService
{
    Task SaveAsync(
        Guid userId,
        CheckLotofacilGamesRequest request,
        LotofacilGameCheckResult result,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GameCheckingHistoryItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class GameCheckingHistoryService(LotoAnalyticsDbContext dbContext) : IGameCheckingHistoryService
{
    // Salva uma conferencia e seus jogos conferidos no historico do usuario.
    public async Task SaveAsync(
        Guid userId,
        CheckLotofacilGamesRequest request,
        LotofacilGameCheckResult result,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var checking = new DbGameChecking
        {
            UserId = userId,
            LotteryModeCode = "lotofacil",
            DrawnNumbers = request.DrawnNumbers.ToArray(),
            GameCount = result.Games.Count,
            AwardSummaryJson = JsonSerializer.Serialize(result.AwardSummary),
            CreatedAt = now,
            Games = result.Games
                .Select(game => new CheckedUserGame
                {
                    GameNumber = game.GameIndex,
                    Numbers = game.Numbers.ToArray(),
                    HitCount = game.HitCount,
                    MatchedNumbers = game.MatchedNumbers.ToArray(),
                    Awarded = game.IsAwarded,
                    CreatedAt = now
                })
                .ToList()
        };

        dbContext.GameCheckings.Add(checking);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Lista as conferencias mais recentes do usuario autenticado.
    public async Task<IReadOnlyList<GameCheckingHistoryItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.GameCheckings
            .AsNoTracking()
            .Where(checking => checking.UserId == userId)
            .OrderByDescending(checking => checking.CreatedAt)
            .Include(checking => checking.Games.OrderBy(game => game.GameNumber))
            .Select(checking => new GameCheckingHistoryItem(
                checking.Id,
                checking.GameCount,
                checking.CreatedAt,
                DeserializeAwardSummary(checking.AwardSummaryJson),
                checking.Games
                    .OrderBy(game => game.GameNumber)
                    .Select(game => new GameCheckingHistoryGameItem(
                        game.GameNumber,
                        game.HitCount,
                        game.MatchedNumbers))
                    .ToArray()))
            .ToArrayAsync(cancellationToken);
    }

    // Desserializa o resumo de premiacao salvo em jsonb.
    private static IReadOnlyDictionary<int, int> DeserializeAwardSummary(string value)
    {
        return JsonSerializer.Deserialize<Dictionary<int, int>>(value) ?? [];
    }
}

public sealed record GameCheckingHistoryItem(
    Guid Id,
    int GameCount,
    DateTimeOffset CreatedAt,
    IReadOnlyDictionary<int, int> AwardSummary,
    IReadOnlyList<GameCheckingHistoryGameItem> Games);

public sealed record GameCheckingHistoryGameItem(
    int GameNumber,
    int HitCount,
    IReadOnlyList<string> MatchedNumbers);
