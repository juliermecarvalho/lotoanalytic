namespace LotoAnalytics.Api.Features.Statistics;

public static class LotofacilStatisticsCalculator
{
    private static readonly HashSet<string> PrimeNumbers = ["02", "03", "05", "07", "11", "13", "17", "19", "23"];

    private static readonly HashSet<string> BorderNumbers =
    [
        "01", "02", "03", "04", "05",
        "06", "10",
        "11", "15",
        "16", "20",
        "21", "22", "23", "24", "25"
    ];

    private static readonly IReadOnlyList<HashSet<string>> Rows =
    [
        ["01", "02", "03", "04", "05"],
        ["06", "07", "08", "09", "10"],
        ["11", "12", "13", "14", "15"],
        ["16", "17", "18", "19", "20"],
        ["21", "22", "23", "24", "25"]
    ];

    private static readonly IReadOnlyList<HashSet<string>> Columns =
    [
        ["01", "06", "11", "16", "21"],
        ["02", "07", "12", "17", "22"],
        ["03", "08", "13", "18", "23"],
        ["04", "09", "14", "19", "24"],
        ["05", "10", "15", "20", "25"]
    ];

    // Calcula as estatisticas da Lotofacil usadas pelo dashboard e pelo gerador.
    public static LotofacilStatistics Calculate(
        IReadOnlyCollection<string> numbers,
        IReadOnlyCollection<string>? previousNumbers)
    {
        var normalizedNumbers = numbers
            .Select(NormalizeNumber)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var previousNumberSet = previousNumbers?
            .Select(NormalizeNumber)
            .ToHashSet(StringComparer.Ordinal) ?? [];
        var repeatedFromPrevious = normalizedNumbers
            .Where(previousNumberSet.Contains)
            .ToArray();
        var evenCount = normalizedNumbers.Count(number => int.Parse(number) % 2 == 0);
        var borderCount = normalizedNumbers.Count(BorderNumbers.Contains);

        return new LotofacilStatistics(
            EvenCount: evenCount,
            OddCount: normalizedNumbers.Length - evenCount,
            NumbersSum: normalizedNumbers.Sum(number => int.Parse(number)),
            RepeatedFromPreviousCount: repeatedFromPrevious.Length,
            RepeatedFromPrevious: repeatedFromPrevious,
            PrimeCount: normalizedNumbers.Count(PrimeNumbers.Contains),
            BorderCount: borderCount,
            CenterCount: normalizedNumbers.Length - borderCount,
            LongestSequence: CalculateLongestSequence(normalizedNumbers),
            RowDistribution: CountGroups(normalizedNumbers, Rows),
            ColumnDistribution: CountGroups(normalizedNumbers, Columns));
    }

    // Normaliza uma dezena para duas casas preservando a representacao textual.
    private static string NormalizeNumber(string number)
    {
        var trimmed = number.Trim();
        return int.TryParse(trimmed, out var parsed) ? parsed.ToString("00") : trimmed;
    }

    // Conta quantas dezenas aparecem em cada grupo fixo da grade da Lotofacil.
    private static int[] CountGroups(IReadOnlyCollection<string> numbers, IReadOnlyList<HashSet<string>> groups)
    {
        var numberSet = numbers.ToHashSet(StringComparer.Ordinal);
        return groups
            .Select(group => group.Count(numberSet.Contains))
            .ToArray();
    }

    // Calcula o maior bloco de dezenas consecutivas dentro do jogo.
    private static int CalculateLongestSequence(IReadOnlyCollection<string> numbers)
    {
        var orderedNumbers = numbers
            .Select(int.Parse)
            .Order()
            .ToArray();

        if (orderedNumbers.Length == 0)
        {
            return 0;
        }

        var longest = 1;
        var current = 1;

        for (var index = 1; index < orderedNumbers.Length; index++)
        {
            if (orderedNumbers[index] == orderedNumbers[index - 1] + 1)
            {
                current++;
                longest = Math.Max(longest, current);
                continue;
            }

            current = 1;
        }

        return longest;
    }
}

public sealed record LotofacilStatistics(
    int EvenCount,
    int OddCount,
    int NumbersSum,
    int RepeatedFromPreviousCount,
    IReadOnlyList<string> RepeatedFromPrevious,
    int PrimeCount,
    int BorderCount,
    int CenterCount,
    int LongestSequence,
    IReadOnlyList<int> RowDistribution,
    IReadOnlyList<int> ColumnDistribution);
