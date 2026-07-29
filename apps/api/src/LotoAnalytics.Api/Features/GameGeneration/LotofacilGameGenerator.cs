namespace LotoAnalytics.Api.Features.GameGeneration;

public static class LotofacilGameGenerator
{
    private const int BoardSize = 25;
    private const int MaxAttempts = 250_000;

    private static readonly HashSet<int> PrimeNumbers = [2, 3, 5, 7, 11, 13, 17, 19, 23];

    // Gera jogos unicos da Lotofacil por amostragem aleatoria aplicando os filtros estatisticos.
    public static LotofacilGameGenerationResult Generate(LotofacilGameGenerationRequest request, Random? random = null)
    {
        ValidateRequest(request);
        random ??= Random.Shared;

        var requiredNumbers = NormalizeNumbers(request.RequiredNumbers);
        var excludedNumbers = NormalizeNumbers(request.ExcludedNumbers);
        var previousNumbers = NormalizeNumbers(request.PreviousNumbers);

        if (requiredNumbers.Overlaps(excludedNumbers))
        {
            throw new ArgumentException("Uma dezena obrigatoria nao pode estar tambem nas excluidas.", nameof(request));
        }

        if (requiredNumbers.Count > request.NumbersPerGame)
        {
            throw new ArgumentException("Ha mais dezenas obrigatorias que o tamanho do jogo.", nameof(request));
        }

        var required = requiredNumbers.Order().ToArray();
        var pool = Enumerable
            .Range(1, BoardSize)
            .Where(number => !requiredNumbers.Contains(number) && !excludedNumbers.Contains(number))
            .ToArray();

        var games = new List<GeneratedLotofacilGame>();
        var seenGames = new HashSet<string>(StringComparer.Ordinal);
        var attempts = 0;

        if (required.Length + pool.Length >= request.NumbersPerGame)
        {
            while (games.Count < request.GameCount && attempts < MaxAttempts)
            {
                attempts++;

                var bag = (int[])pool.Clone();
                for (var index = bag.Length - 1; index > 0; index--)
                {
                    var swap = random.Next(index + 1);
                    (bag[index], bag[swap]) = (bag[swap], bag[index]);
                }

                var numbers = required
                    .Concat(bag.Take(request.NumbersPerGame - required.Length))
                    .Order()
                    .ToArray();
                var key = string.Join(",", numbers);
                if (seenGames.Contains(key))
                {
                    continue;
                }

                var metrics = ComputeMetrics(numbers, previousNumbers);
                if (!PassesFilters(metrics, request))
                {
                    continue;
                }

                if (request.ForbiddenGameKeys is not null && request.ForbiddenGameKeys.Contains(FormatGameKey(numbers)))
                {
                    continue;
                }

                seenGames.Add(key);
                games.Add(new GeneratedLotofacilGame(
                    Numbers: numbers.Select(number => number.ToString("00")).ToArray(),
                    EvenCount: metrics.EvenCount,
                    OddCount: numbers.Length - metrics.EvenCount,
                    NumbersSum: metrics.Sum,
                    RepeatedFromPreviousCount: metrics.RepeatedCount,
                    PrimeCount: metrics.PrimeCount,
                    BorderCount: metrics.BorderCount,
                    LongestSequence: metrics.MaxSequence));
            }
        }

        return new LotofacilGameGenerationResult(games, attempts);
    }

    // Monta a chave canonica de um jogo no formato usado para comparar com sorteios da base.
    public static string FormatGameKey(IReadOnlyCollection<int> numbers)
    {
        return string.Join(",", numbers.Order().Select(number => number.ToString("00")));
    }

    // Calcula as metricas estatisticas de um jogo com dezenas ordenadas.
    private static GameMetrics ComputeMetrics(int[] numbers, HashSet<int> previousNumbers)
    {
        var evenCount = 0;
        var sum = 0;
        var repeatedCount = 0;
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

            if (previousNumbers.Contains(number))
            {
                repeatedCount++;
            }

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

        return new GameMetrics(
            EvenCount: evenCount,
            Sum: sum,
            RepeatedCount: repeatedCount,
            PrimeCount: primeCount,
            BorderCount: numbers.Length - centerCount,
            MaxSequence: maxSequence,
            MinPerRowColumn: allCounts.Min(),
            MaxPerRowColumn: allCounts.Max());
    }

    // Verifica se as metricas do jogo atendem a todos os filtros informados na requisicao.
    private static bool PassesFilters(GameMetrics metrics, LotofacilGameGenerationRequest request)
    {
        if (request.EvenCount is not null && metrics.EvenCount != request.EvenCount)
        {
            return false;
        }

        if (request.OddCount is not null && request.NumbersPerGame - metrics.EvenCount != request.OddCount)
        {
            return false;
        }

        if (request.MinimumSum is not null && metrics.Sum < request.MinimumSum)
        {
            return false;
        }

        if (request.MaximumSum is not null && metrics.Sum > request.MaximumSum)
        {
            return false;
        }

        if (request.SumRanges.Count > 0 &&
            !request.SumRanges.Any(range => metrics.Sum >= range.MinimumSum && metrics.Sum <= range.MaximumSum))
        {
            return false;
        }

        if (request.MinimumRepeated is not null && metrics.RepeatedCount < request.MinimumRepeated)
        {
            return false;
        }

        if (request.MaximumRepeated is not null && metrics.RepeatedCount > request.MaximumRepeated)
        {
            return false;
        }

        if (request.MinimumPrimes is not null && metrics.PrimeCount < request.MinimumPrimes)
        {
            return false;
        }

        if (request.MaximumPrimes is not null && metrics.PrimeCount > request.MaximumPrimes)
        {
            return false;
        }

        if (request.MinimumBorder is not null && metrics.BorderCount < request.MinimumBorder)
        {
            return false;
        }

        if (request.MaximumBorder is not null && metrics.BorderCount > request.MaximumBorder)
        {
            return false;
        }

        if (request.MinimumPerRowColumn is not null && metrics.MinPerRowColumn < request.MinimumPerRowColumn)
        {
            return false;
        }

        if (request.MaximumPerRowColumn is not null && metrics.MaxPerRowColumn > request.MaximumPerRowColumn)
        {
            return false;
        }

        if (request.MaximumSequence is not null && metrics.MaxSequence > request.MaximumSequence)
        {
            return false;
        }

        return true;
    }

    // Valida limites basicos aceitos pela Lotofacil.
    private static void ValidateRequest(LotofacilGameGenerationRequest request)
    {
        if (request.GameCount is < 1 or > 100)
        {
            throw new ArgumentException("A quantidade de jogos deve estar entre 1 e 100.", nameof(request));
        }

        if (request.NumbersPerGame is not (15 or 16))
        {
            throw new ArgumentException("Cada jogo deve ter 15 ou 16 dezenas.", nameof(request));
        }

        if (request.EvenCount is not null && request.EvenCount is < 0 or > 16)
        {
            throw new ArgumentException("A quantidade de pares e invalida.", nameof(request));
        }

        if (request.OddCount is not null && request.OddCount is < 0 or > 16)
        {
            throw new ArgumentException("A quantidade de impares e invalida.", nameof(request));
        }

        if (request.EvenCount is not null && request.OddCount is not null && request.EvenCount + request.OddCount != request.NumbersPerGame)
        {
            throw new ArgumentException("A soma de pares e impares deve bater com o tamanho do jogo.", nameof(request));
        }

        if (request.SumRanges.Any(range => range.MinimumSum > range.MaximumSum))
        {
            throw new ArgumentException("Cada faixa de soma deve ter minimo menor ou igual ao maximo.", nameof(request));
        }
    }

    // Normaliza dezenas texto para inteiros validando o intervalo oficial de 01 a 25.
    private static HashSet<int> NormalizeNumbers(IReadOnlyCollection<string> numbers)
    {
        return numbers
            .Select(number =>
            {
                if (!int.TryParse(number.Trim(), out var parsed) || parsed is < 1 or > BoardSize)
                {
                    throw new ArgumentException("A dezena deve estar entre 01 e 25.", nameof(numbers));
                }

                return parsed;
            })
            .ToHashSet();
    }

    private readonly record struct GameMetrics(
        int EvenCount,
        int Sum,
        int RepeatedCount,
        int PrimeCount,
        int BorderCount,
        int MaxSequence,
        int MinPerRowColumn,
        int MaxPerRowColumn);
}

public sealed record LotofacilGameGenerationRequest
{
    public required int GameCount { get; init; }

    public int NumbersPerGame { get; init; } = 15;

    public IReadOnlyCollection<string> RequiredNumbers { get; init; } = [];

    public IReadOnlyCollection<string> ExcludedNumbers { get; init; } = [];

    public IReadOnlyCollection<string> PreviousNumbers { get; init; } = [];

    public int? EvenCount { get; init; }

    public int? OddCount { get; init; }

    public int? MinimumSum { get; init; }

    public int? MaximumSum { get; init; }

    public IReadOnlyList<SumRangeFilter> SumRanges { get; init; } = [];

    public int? MinimumRepeated { get; init; }

    public int? MaximumRepeated { get; init; }

    public int? MinimumPrimes { get; init; }

    public int? MaximumPrimes { get; init; }

    public int? MinimumBorder { get; init; }

    public int? MaximumBorder { get; init; }

    public int? MinimumPerRowColumn { get; init; }

    public int? MaximumPerRowColumn { get; init; }

    public int? MaximumSequence { get; init; }

    public IReadOnlySet<string>? ForbiddenGameKeys { get; init; }
}

public sealed record SumRangeFilter(int MinimumSum, int MaximumSum);

public sealed record LotofacilGameGenerationResult(
    IReadOnlyList<GeneratedLotofacilGame> Games,
    int AttemptCount);

public sealed record GeneratedLotofacilGame(
    IReadOnlyList<string> Numbers,
    int EvenCount,
    int OddCount,
    int NumbersSum,
    int RepeatedFromPreviousCount,
    int PrimeCount,
    int BorderCount,
    int LongestSequence);
