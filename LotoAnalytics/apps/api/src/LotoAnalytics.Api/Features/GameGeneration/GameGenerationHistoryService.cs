using System.Text.Json;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using DbGameGeneration = LotoAnalytics.Api.Infrastructure.Database.GameGeneration;

namespace LotoAnalytics.Api.Features.GameGeneration;

public interface IGameGenerationHistoryService
{
    Task SaveAsync(
        Guid userId,
        GenerateLotofacilGamesRequest request,
        LotofacilGameGenerationResult result,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<GameGenerationHistoryItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<GameGenerationHistoryItem?> GetByUserAsync(Guid userId, Guid generationId, CancellationToken cancellationToken);
}

public sealed class GameGenerationHistoryService(LotoAnalyticsDbContext dbContext) : IGameGenerationHistoryService
{
    // Salva a geracao e os jogos gerados no historico do usuario.
    public async Task SaveAsync(
        Guid userId,
        GenerateLotofacilGamesRequest request,
        LotofacilGameGenerationResult result,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var generation = new DbGameGeneration
        {
            UserId = userId,
            LotteryModeCode = "lotofacil",
            GameCount = result.Games.Count,
            NumbersPerGame = request.NumbersPerGame,
            FiltersJson = JsonSerializer.Serialize(request),
            CreatedAt = now,
            Games = result.Games
                .Select((game, index) => new GeneratedGame
                {
                    GameNumber = index + 1,
                    Numbers = game.Numbers.ToArray(),
                    EvenCount = game.EvenCount,
                    OddCount = game.OddCount,
                    NumbersSum = game.NumbersSum,
                    CreatedAt = now
                })
                .ToList()
        };

        dbContext.GameGenerations.Add(generation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Lista as geracoes mais recentes do usuario autenticado.
    public async Task<IReadOnlyList<GameGenerationHistoryItem>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.GameGenerations
            .AsNoTracking()
            .Where(generation => generation.UserId == userId)
            .OrderByDescending(generation => generation.CreatedAt)
            .Include(generation => generation.Games.OrderBy(game => game.GameNumber))
            .Select(generation => new GameGenerationHistoryItem(
                generation.Id,
                generation.GameCount,
                generation.NumbersPerGame,
                generation.CreatedAt,
                generation.Games
                    .OrderBy(game => game.GameNumber)
                    .Select(game => new GameGenerationHistoryGameItem(
                        game.GameNumber,
                        game.Numbers,
                        game.NumbersSum))
                    .ToArray()))
            .ToArrayAsync(cancellationToken);
    }

    // Busca uma geracao especifica pertencente ao usuario autenticado.
    public async Task<GameGenerationHistoryItem?> GetByUserAsync(Guid userId, Guid generationId, CancellationToken cancellationToken)
    {
        return await dbContext.GameGenerations
            .AsNoTracking()
            .Where(generation => generation.UserId == userId && generation.Id == generationId)
            .Include(generation => generation.Games.OrderBy(game => game.GameNumber))
            .Select(generation => new GameGenerationHistoryItem(
                generation.Id,
                generation.GameCount,
                generation.NumbersPerGame,
                generation.CreatedAt,
                generation.Games
                    .OrderBy(game => game.GameNumber)
                    .Select(game => new GameGenerationHistoryGameItem(
                        game.GameNumber,
                        game.Numbers,
                        game.NumbersSum))
                    .ToArray()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

public sealed record GameGenerationHistoryItem(
    Guid Id,
    int GameCount,
    int NumbersPerGame,
    DateTimeOffset CreatedAt,
    IReadOnlyList<GameGenerationHistoryGameItem> Games);

public sealed record GameGenerationHistoryGameItem(
    int GameNumber,
    IReadOnlyList<string> Numbers,
    int NumbersSum);
