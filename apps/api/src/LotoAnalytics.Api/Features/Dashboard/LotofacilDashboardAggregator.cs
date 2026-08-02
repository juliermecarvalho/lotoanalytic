using LotoAnalytics.Api.Features.FilterStatistics;
using LotoAnalytics.Api.Features.GameGeneration;

namespace LotoAnalytics.Api.Features.Dashboard;

public static class LotofacilDashboardAggregator
{
    // Configuracao do volante 5x5 da Lotofacil, usada por padrao para manter a compatibilidade.
    public static readonly DashboardBoardConfig LotofacilConfig = new()
    {
        Board = LotofacilGameGenerator.Board,
        PreferredSumLowerBound = 185,
        PreferredSumUpperBound = 210,
        IncludeGrid = true
    };

    // Consolida frequencias, atrasos, KPIs e distribuicoes de filtro do volante 5x5 da Lotofacil.
    public static DashboardSnapshot Aggregate(IReadOnlyList<DashboardDraw> orderedDraws)
    {
        return Aggregate(orderedDraws, LotofacilConfig);
    }

    // Consolida o painel estatistico para a cartela descrita em config.
    public static DashboardSnapshot Aggregate(IReadOnlyList<DashboardDraw> orderedDraws, DashboardBoardConfig config)
    {
        var boardSize = config.Board.BoardSize;
        if (orderedDraws.Count == 0)
        {
            return new DashboardSnapshot(
                TotalContests: 0,
                LatestContest: null,
                Summary: new DashboardSummary(0, 0, 0, 0),
                Frequencies: [],
                Categories: new Dictionary<string, IReadOnlyList<DashboardCategoryItem>>());
        }

        var frequency = new int[boardSize + 1];
        var lastSeenContest = new int[boardSize + 1];
        var lastSeenIndex = new int[boardSize + 1];
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
                if (number >= 1 && number <= boardSize)
                {
                    frequency[number]++;
                    lastSeenContest[number] = draw.ContestNumber;
                    lastSeenIndex[number] = index;
                }

                sum += number;
            }

            sumTotal += sum;
            if (sum >= config.PreferredSumLowerBound && sum <= config.PreferredSumUpperBound)
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

        var frequencies = new List<DashboardNumberFrequency>(boardSize);
        for (var number = 1; number <= boardSize; number++)
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
        var latestSummary = BuildLatestContest(latest, latestNumbers, previousDraw, config.Board);

        var uniquePercentage = Percentage(uniqueCombinations.Count, total);
        var summary = new DashboardSummary(
            AverageSum: Math.Round(sumTotal / (double)total, 1),
            AverageRepetition: repetitionSamples == 0 ? 0 : Math.Round(repetitionTotal / (double)repetitionSamples, 1),
            UniqueCombinationsPercentage: uniquePercentage,
            PreferredSumPercentage: Percentage(preferredSumCount, total));

        var buckets = FilterStatisticsAggregator.Aggregate(
            orderedDraws.Select(draw => (IReadOnlyList<int>)draw.Numbers).ToArray(),
            config.Board,
            config.IncludeGrid);

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
    private static DashboardLatestContest BuildLatestContest(DashboardDraw latest, int[] numbers, int[]? previous, BoardSpec board)
    {
        var evenCount = numbers.Count(number => number % 2 == 0);
        var primeCount = numbers.Count(board.PrimeNumbers.Contains);
        // A moldura so existe em cartelas com miolo definido (Lotofacil); modalidades sem miolo retornam 0.
        var borderCount = board.IsCenterCell is null ? 0 : numbers.Count(number => IsBorder(number, board));
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

    // Indica se a dezena esta na moldura do volante (fora do miolo definido pela cartela).
    private static bool IsBorder(int number, BoardSpec board)
    {
        var row = (number - 1) / board.Columns;
        var column = (number - 1) % board.Columns;
        return board.IsCenterCell is null || !board.IsCenterCell(row, column);
    }

    // Converte uma contagem em percentual arredondado com uma casa decimal.
    private static double Percentage(int count, int total)
    {
        return total == 0 ? 0 : Math.Round(count * 100.0 / total, 1);
    }
}

// Configuracao de cartela para o painel estatistico de uma modalidade.
public sealed record DashboardBoardConfig
{
    public required BoardSpec Board { get; init; }

    // Faixa de soma considerada "preferencial" no KPI do painel.
    public required int PreferredSumLowerBound { get; init; }

    public required int PreferredSumUpperBound { get; init; }

    // Inclui a distribuicao de grade (linha/coluna) nas categorias; so faz sentido em volantes densos.
    public required bool IncludeGrid { get; init; }
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
