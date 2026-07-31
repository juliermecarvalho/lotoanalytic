using LotoAnalytics.Api.Features.FilterStatistics;

namespace LotoAnalytics.Api.Features.Dashboard;

public static class LotofacilDashboardAggregator
{
    private const int BoardSize = 25;
    private const int PreferredSumLowerBound = 185;
    private const int PreferredSumUpperBound = 210;

    private static readonly HashSet<int> PrimeNumbers = [2, 3, 5, 7, 11, 13, 17, 19, 23];

    // Consolida frequencias, atrasos, KPIs e distribuicoes de filtro a partir dos sorteios ordenados.
    public static DashboardSnapshot Aggregate(IReadOnlyList<DashboardDraw> orderedDraws)
    {
        if (orderedDraws.Count == 0)
        {
            return new DashboardSnapshot(
                TotalContests: 0,
                LatestContest: null,
                Summary: new DashboardSummary(0, 0, 0, 0),
                Frequencies: [],
                Categories: new Dictionary<string, IReadOnlyList<DashboardCategoryItem>>());
        }

        var frequency = new int[BoardSize + 1];
        var lastSeenContest = new int[BoardSize + 1];
        var lastSeenIndex = new int[BoardSize + 1];
        Array.Fill(lastSeenContest, 0);
        Array.Fill(lastSeenIndex, -1);

        var sumTotal = 0L;
        var repetitionTotal = 0L;
        var repetitionSamples = 0;
        var preferredSumCount = 0;
        var uniqueCombinations = new HashSet<string>();
        var repeatedCombinations = 0;
        int[]? previousNumbers = null;
        var lastIndex = orderedDraws.Count - 1;

        for (var index = 0; index < orderedDraws.Count; index++)
        {
            var draw = orderedDraws[index];
            var numbers = draw.Numbers.Order().ToArray();

            var sum = 0;
            foreach (var number in numbers)
            {
                if (number is >= 1 and <= BoardSize)
                {
                    frequency[number]++;
                    lastSeenContest[number] = draw.ContestNumber;
                    lastSeenIndex[number] = index;
                }

                sum += number;
            }

            sumTotal += sum;
            if (sum is >= PreferredSumLowerBound and <= PreferredSumUpperBound)
            {
                preferredSumCount++;
            }

            var key = string.Join('-', numbers);
            if (!uniqueCombinations.Add(key))
            {
                repeatedCombinations++;
            }

            if (previousNumbers is not null)
            {
                var previousSet = previousNumbers.ToHashSet();
                repetitionTotal += numbers.Count(previousSet.Contains);
                repetitionSamples++;
            }

            previousNumbers = numbers;
        }

        var total = orderedDraws.Count;

        var frequencies = new List<DashboardNumberFrequency>(BoardSize);
        for (var number = 1; number <= BoardSize; number++)
        {
            var count = frequency[number];
            var percentage = Percentage(count, total);
            // Atraso: quantos concursos seguidos, a partir do mais recente, a dezena ficou de fora.
            var delay = lastSeenIndex[number] < 0 ? total : lastIndex - lastSeenIndex[number];
            var lastContest = lastSeenContest[number] == 0 ? (int?)null : lastSeenContest[number];
            frequencies.Add(new DashboardNumberFrequency(number, count, percentage, delay, lastContest));
        }

        var latest = orderedDraws[^1];
        var latestNumbers = latest.Numbers.Order().ToArray();
        var previousDraw = orderedDraws.Count >= 2 ? orderedDraws[^2].Numbers.Order().ToArray() : null;
        var latestSummary = BuildLatestContest(latest, latestNumbers, previousDraw);

        var uniquePercentage = Percentage(uniqueCombinations.Count, total);
        var summary = new DashboardSummary(
            AverageSum: Math.Round(sumTotal / (double)total, 1),
            AverageRepetition: repetitionSamples == 0 ? 0 : Math.Round(repetitionTotal / (double)repetitionSamples, 1),
            UniqueCombinationsPercentage: uniquePercentage,
            PreferredSumPercentage: Percentage(preferredSumCount, total));

        var buckets = FilterStatisticsAggregator.Aggregate(
            orderedDraws.Select(draw => (IReadOnlyList<int>)draw.Numbers).ToArray());

        var categories = buckets
            .GroupBy(bucket => bucket.Category)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<DashboardCategoryItem>)group
                    .OrderBy(bucket => bucket.Value)
                    .Select(bucket => new DashboardCategoryItem(bucket.Value, bucket.Count))
                    .ToArray());

        return new DashboardSnapshot(total, latestSummary, summary, frequencies, categories);
    }

    // Calcula as metricas do ultimo concurso para alimentar o card lateral do painel.
    private static DashboardLatestContest BuildLatestContest(DashboardDraw latest, int[] numbers, int[]? previous)
    {
        var evenCount = numbers.Count(number => number % 2 == 0);
        var primeCount = numbers.Count(PrimeNumbers.Contains);
        var borderCount = numbers.Count(IsBorder);
        var repeated = previous is null ? 0 : numbers.Count(previous.Contains);

        return new DashboardLatestContest(
            ContestNumber: latest.ContestNumber,
            DrawDate: latest.DrawDate,
            Numbers: numbers.Select(number => number.ToString("00")).ToArray(),
            EvenCount: evenCount,
            OddCount: numbers.Length - evenCount,
            Sum: numbers.Sum(),
            PrimeCount: primeCount,
            BorderCount: borderCount,
            RepeatedFromPrevious: repeated);
    }

    // Indica se a dezena esta na moldura do volante 5x5 (linha ou coluna nas bordas).
    private static bool IsBorder(int number)
    {
        var row = (number - 1) / 5;
        var column = (number - 1) % 5;
        return row is 0 or 4 || column is 0 or 4;
    }

    // Converte uma contagem em percentual arredondado com uma casa decimal.
    private static double Percentage(int count, int total)
    {
        return total == 0 ? 0 : Math.Round(count * 100.0 / total, 1);
    }
}

public sealed record DashboardDraw(int ContestNumber, DateOnly? DrawDate, IReadOnlyList<int> Numbers);

public sealed record DashboardSnapshot(
    int TotalContests,
    DashboardLatestContest? LatestContest,
    DashboardSummary Summary,
    IReadOnlyList<DashboardNumberFrequency> Frequencies,
    IReadOnlyDictionary<string, IReadOnlyList<DashboardCategoryItem>> Categories);

public sealed record DashboardSummary(
    double AverageSum,
    double AverageRepetition,
    double UniqueCombinationsPercentage,
    double PreferredSumPercentage);

public sealed record DashboardNumberFrequency(
    int Number,
    int Count,
    double Percentage,
    int Delay,
    int? LastContest);

public sealed record DashboardLatestContest(
    int ContestNumber,
    DateOnly? DrawDate,
    IReadOnlyList<string> Numbers,
    int EvenCount,
    int OddCount,
    int Sum,
    int PrimeCount,
    int BorderCount,
    int RepeatedFromPrevious);

public sealed record DashboardCategoryItem(int Value, int Count);
