using LotoAnalytics.Api.Features.GameGeneration;

namespace LotoAnalytics.Api.Features.FilterStatistics;

public static class FilterStatisticsAggregator
{
    public const string ParityCategory = "paridade";
    public const string RepetitionCategory = "repeticao";
    public const string PrimesCategory = "primos";
    public const string BorderCategory = "moldura";
    public const string SumCategory = "soma";
    public const string GridCategory = "grade";
    public const string SequenceCategory = "sequencia";

    // Agrega as distribuicoes das estatisticas de filtro do volante 5x5 da Lotofacil (compatibilidade).
    public static IReadOnlyList<FilterStatisticBucket> Aggregate(IReadOnlyList<IReadOnlyList<int>> orderedDraws)
    {
        return Aggregate(orderedDraws, LotofacilGameGenerator.Board, includeGrid: true);
    }

    // Agrega as distribuicoes das estatisticas de filtro para a cartela informada.
    // A moldura so e computada quando a cartela define o miolo; a grade linha/coluna e opcional.
    public static IReadOnlyList<FilterStatisticBucket> Aggregate(
        IReadOnlyList<IReadOnlyList<int>> orderedDraws,
        BoardSpec spec,
        bool includeGrid)
    {
        var counters = new Dictionary<(string Category, int Value), int>();
        HashSet<int>? previousDraw = null;

        foreach (var draw in orderedDraws)
        {
            var numbers = draw.Order().ToArray();
            var metrics = ComputeMetrics(numbers, spec, includeGrid);

            Increment(counters, ParityCategory, metrics.EvenCount);
            Increment(counters, PrimesCategory, metrics.PrimeCount);
            Increment(counters, SumCategory, metrics.Sum);
            Increment(counters, SequenceCategory, metrics.MaxSequence);

            if (metrics.BorderCount is not null)
            {
                Increment(counters, BorderCategory, metrics.BorderCount.Value);
            }

            if (metrics.GridClass is not null)
            {
                Increment(counters, GridCategory, metrics.GridClass.Value);
            }

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

    // Calcula as metricas de um sorteio com dezenas ordenadas para a cartela informada.
    private static DrawMetrics ComputeMetrics(int[] numbers, BoardSpec spec, bool includeGrid)
    {
        var rows = spec.BoardSize / spec.Columns;
        var evenCount = 0;
        var sum = 0;
        var primeCount = 0;
        var centerCount = 0;
        var maxSequence = 0;
        var currentSequence = 0;
        var previousValue = int.MinValue;
        var rowCounts = new int[rows];
        var columnCounts = new int[spec.Columns];

        foreach (var number in numbers)
        {
            if (number % 2 == 0)
            {
                evenCount++;
            }

            sum += number;

            if (spec.PrimeNumbers.Contains(number))
            {
                primeCount++;
            }

            var row = (number - 1) / spec.Columns;
            var column = (number - 1) % spec.Columns;
            rowCounts[row]++;
            columnCounts[column]++;
            if (spec.IsCenterCell is not null && spec.IsCenterCell(row, column))
            {
                centerCount++;
            }

            currentSequence = number == previousValue + 1 ? currentSequence + 1 : 1;
            maxSequence = Math.Max(maxSequence, currentSequence);
            previousValue = number;
        }

        int? gridClass = null;
        if (includeGrid)
        {
            var allCounts = rowCounts.Concat(columnCounts).ToArray();
            var min = allCounts.Min();
            var max = allCounts.Max();
            gridClass = min == 0 ? 3 : max >= 5 ? 2 : min == 1 ? 1 : 0;
        }

        return new DrawMetrics(
            EvenCount: evenCount,
            Sum: sum,
            PrimeCount: primeCount,
            BorderCount: spec.IsCenterCell is null ? null : numbers.Length - centerCount,
            MaxSequence: maxSequence,
            GridClass: gridClass);
    }

    private readonly record struct DrawMetrics(
        int EvenCount,
        int Sum,
        int PrimeCount,
        int? BorderCount,
        int MaxSequence,
        int? GridClass);
}

public sealed record FilterStatisticBucket(string Category, int Value, int Count);
