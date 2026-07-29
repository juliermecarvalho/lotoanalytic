namespace LotoAnalytics.Api.Features.GameChecking;

public static class LotofacilGameChecker
{
    private static readonly IReadOnlyList<int> AwardHitCounts = [15, 14, 13, 12, 11];

    // Confere os jogos do usuario contra o resultado oficial da Lotofacil.
    public static LotofacilGameCheckResult Check(
        IReadOnlyCollection<string> drawnNumbers,
        IReadOnlyList<IReadOnlyCollection<string>> games)
    {
        var normalizedDrawnNumbers = NormalizeNumbers(drawnNumbers, expectedCount: 15, fieldName: nameof(drawnNumbers));
        var drawnNumberSet = normalizedDrawnNumbers.ToHashSet(StringComparer.Ordinal);
        var checkedGames = games
            .Select((game, index) => CheckGame(index + 1, game, drawnNumberSet))
            .ToArray();
        var awardSummary = AwardHitCounts.ToDictionary(
            hitCount => hitCount,
            hitCount => checkedGames.Count(game => game.HitCount == hitCount));

        return new LotofacilGameCheckResult(checkedGames, awardSummary);
    }

    // Confere um jogo individual e preserva a ordem normalizada das dezenas.
    private static LotofacilCheckedGame CheckGame(
        int gameIndex,
        IReadOnlyCollection<string> game,
        HashSet<string> drawnNumberSet)
    {
        var normalizedGame = NormalizeNumbers(game, expectedCount: null, fieldName: $"games[{gameIndex - 1}]");

        if (normalizedGame.Length is not (15 or 16))
        {
            throw new ArgumentException("O jogo deve conter 15 ou 16 dezenas.", nameof(game));
        }

        var matchedNumbers = normalizedGame
            .Where(drawnNumberSet.Contains)
            .ToArray();

        return new LotofacilCheckedGame(
            GameIndex: gameIndex,
            Numbers: normalizedGame,
            HitCount: matchedNumbers.Length,
            MatchedNumbers: matchedNumbers,
            IsAwarded: matchedNumbers.Length is >= 11 and <= 15);
    }

    // Normaliza e valida dezenas no intervalo oficial da Lotofacil.
    private static string[] NormalizeNumbers(
        IReadOnlyCollection<string> numbers,
        int? expectedCount,
        string fieldName)
    {
        if (expectedCount is not null && numbers.Count != expectedCount)
        {
            throw new ArgumentException($"O campo {fieldName} deve conter {expectedCount} dezenas.", fieldName);
        }

        var normalizedNumbers = numbers
            .Select(NormalizeNumber)
            .Order(StringComparer.Ordinal)
            .ToArray();

        if (normalizedNumbers.Distinct(StringComparer.Ordinal).Count() != normalizedNumbers.Length)
        {
            throw new ArgumentException($"O campo {fieldName} contem dezenas duplicadas.", fieldName);
        }

        return normalizedNumbers;
    }

    // Converte a dezena para duas casas e rejeita valores fora de 01 a 25.
    private static string NormalizeNumber(string number)
    {
        if (!int.TryParse(number.Trim(), out var parsed) || parsed is < 1 or > 25)
        {
            throw new ArgumentException("A dezena deve estar entre 01 e 25.", nameof(number));
        }

        return parsed.ToString("00");
    }
}

public sealed record LotofacilGameCheckResult(
    IReadOnlyList<LotofacilCheckedGame> Games,
    IReadOnlyDictionary<int, int> AwardSummary);

public sealed record LotofacilCheckedGame(
    int GameIndex,
    IReadOnlyList<string> Numbers,
    int HitCount,
    IReadOnlyList<string> MatchedNumbers,
    bool IsAwarded);
