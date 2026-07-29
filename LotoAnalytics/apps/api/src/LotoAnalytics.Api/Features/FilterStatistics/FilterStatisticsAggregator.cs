namespace LotoAnalytics.Api.Features.FilterStatistics;

public static class FilterStatisticsAggregator
{
    private static readonly HashSet<int> PrimeNumbers = [2, 3, 5, 7, 11, 13, 17, 19, 23];

    public const string ParityCategory = "paridade";
    public const string RepetitionCategory = "repeticao";
    public const string PrimesCategory = "primos";
    public const string BorderCategory = "moldura";
    public const string SumCategory = "soma";
    public const string GridCategory = "grade";
    public const string SequenceCategory = "sequencia";

    // Agrega as distribuicoes das oito estatisticas de filtro a partir dos sorteios ordenados.
    public static IReadOnlyList<FilterStatisticBucket> Aggregate(IReadOnlyList<IReadOnlyList<int>> orderedDraws)
    {
        var counters = new Dictionary<(string Category, int Value), int>();
        HashSet<int>? previousDraw = null;

        foreach (var draw in orderedDraws)
        {
            var numbers = draw.Order().ToArray();
            var metrics = ComputeMetrics(numbers);

            Increment(counters, ParityCategory, metrics.EvenCount);
            Increment(counters, PrimesCategory, metrics.PrimeCount);
            Increment(counters, BorderCategory, metrics.BorderCount);
            Increment(counters, SumCategory, metrics.Sum);
            Increment(counters, GridCategory, metrics.GridClass);
            Increment(counters, SequenceCategory, metrics.MaxSequence);

            if (previousDraw is not null)
            {
                Increment(counters, RepetitionCategory, numbers.Count(previousDraw.Contains));
            }

            previousDraw = [.. numbers];
        }

        return counters
            .OrderBy(counter => counter.Key.Category, StringComparer.Ordinal)
            .ThenBy(counter => counter.Key.Value)
            .Select(counter => new FilterStatisticBucket(counter.Key.Category, counter.Key.Value, counter.Value))
            .ToArray();
    }

    // Soma uma ocorrencia no balde da categoria e valor informados.
    private static void Increment(Dictionary<(string, int), int> counters, string category, int value)
    {
        counters[(category, value)] = counters.GetValueOrDefault((category, value)) + 1;
    }

    // Calcula as metricas de um sorteio com dezenas ordenadas.
    private static DrawMetrics ComputeMetrics(int[] numbers)
    {
        var evenCount = 0;
        var sum = 0;
        var primeCount = 0;
        var centerCount = 0;
        var maxSequence = 0;
        var currentSequence = 0;
        var previousValue = int.MinValue;
        var rowCounts = new int[5];
        var columnCounts = new int[5];

        foreach (var number in numbers)
        {
            if (number % 2 == 0)
            {
                evenCount++;
            }

            sum += number;

            if (PrimeNumbers.Contains(number))
            {
                primeCount++;
            }

            var row = (number - 1) / 5;
            var column = (number - 1) % 5;
            rowCounts[row]++;
            columnCounts[column]++;
            if (row is >= 1 and <= 3 && column is >= 1 and <= 3)
            {
                centerCount++;
            }

            currentSequence = number == previousValue + 1 ? currentSequence + 1 : 1;
            maxSequence = Math.Max(maxSequence, currentSequence);
            previousValue = number;
        }

        var allCounts = rowCounts.Concat(columnCounts).ToArray();
        var min = allCounts.Min();
        var max = allCounts.Max();
        var gridClass = min == 0 ? 3 : max >= 5 ? 2 : min == 1 ? 1 : 0;

        return new DrawMetrics(
            EvenCount: evenCount,
            Sum: sum,
            PrimeCount: primeCount,
            BorderCount: numbers.Length - centerCount,
            MaxSequence: maxSequence,
            GridClass: gridClass);
    }

    private readonly record struct DrawMetrics(
        int EvenCount,
        int Sum,
        int PrimeCount,
        int BorderCount,
        int MaxSequence,
        int GridClass);
}

public sealed record FilterStatisticBucket(string Category, int Value, int Count);
