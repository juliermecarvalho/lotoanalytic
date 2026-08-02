namespace LotoAnalytics.Api.Features.GameGeneration;

// Nucleo compartilhado de geracao de jogos por amostragem aleatoria com filtros estatisticos.
// Cada modalidade (Lotofacil, Mega-Sena, ...) fornece um BoardSpec com o tamanho da cartela,
// a grade linha/coluna, o conjunto de primos e a regra de moldura; os geradores especificos
// apenas traduzem seus contratos PT-BR para CoreGameRequest e mapeiam o resultado de volta.
public static class GameGeneratorCore
{
    private const int MaxAttempts = 250_000;

    // Gera jogos unicos aplicando todos os filtros da requisicao para a cartela descrita em spec.
    public static CoreGameGenerationResult Generate(BoardSpec spec, CoreGameRequest request, Random? random = null)
    {
        ValidateRequest(spec, request);
        random ??= Random.Shared;

        var requiredNumbers = NormalizeNumbers(spec, request.RequiredNumbers);
        var excludedNumbers = NormalizeNumbers(spec, request.ExcludedNumbers);
        var previousNumbers = NormalizeNumbers(spec, request.PreviousNumbers);

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
            .Range(spec.FirstNumber, spec.BoardSize)
            .Where(number => !requiredNumbers.Contains(number) && !excludedNumbers.Contains(number))
            .ToArray();

        var games = new List<CoreGame>();
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

                var metrics = ComputeMetrics(spec, numbers, previousNumbers);
                if (!PassesFilters(metrics, request))
                {
                    continue;
                }

                if (request.ForbiddenGameKeys is not null && request.ForbiddenGameKeys.Contains(FormatGameKey(numbers)))
                {
                    continue;
                }

                seenGames.Add(key);
                games.Add(new CoreGame(
                    Numbers: numbers,
                    EvenCount: metrics.EvenCount,
                    OddCount: numbers.Length - metrics.EvenCount,
                    NumbersSum: metrics.Sum,
                    RepeatedFromPreviousCount: metrics.RepeatedCount,
                    PrimeCount: metrics.PrimeCount,
                    BorderCount: metrics.BorderCount,
                    LongestSequence: metrics.MaxSequence));
            }
        }

        return new CoreGameGenerationResult(games, attempts);
    }

    // Monta a chave canonica de um jogo no formato usado para comparar com sorteios da base.
    public static string FormatGameKey(IReadOnlyCollection<int> numbers)
    {
        return string.Join(",", numbers.Order().Select(number => number.ToString("00")));
    }

    // Calcula as metricas estatisticas de um jogo com dezenas ordenadas para a cartela informada.
    private static GameMetrics ComputeMetrics(BoardSpec spec, int[] numbers, HashSet<int> previousNumbers)
    {
        var rows = spec.BoardSize / spec.Columns;
        var evenCount = 0;
        var sum = 0;
        var repeatedCount = 0;
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

            if (previousNumbers.Contains(number))
            {
                repeatedCount++;
            }

            if (spec.PrimeNumbers.Contains(number))
            {
                primeCount++;
            }

            var row = (number - spec.FirstNumber) / spec.Columns;
            var column = (number - spec.FirstNumber) % spec.Columns;
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

        var allCounts = rowCounts.Concat(columnCounts).ToArray();

        return new GameMetrics(
            EvenCount: evenCount,
            Sum: sum,
            RepeatedCount: repeatedCount,
            PrimeCount: primeCount,
            BorderCount: spec.IsCenterCell is null ? 0 : numbers.Length - centerCount,
            MaxSequence: maxSequence,
            MinPerRowColumn: allCounts.Min(),
            MaxPerRowColumn: allCounts.Max());
    }

    // Verifica se as metricas do jogo atendem a todos os filtros informados na requisicao.
    private static bool PassesFilters(GameMetrics metrics, CoreGameRequest request)
    {
        if (request.EvenCount is not null && metrics.EvenCount != request.EvenCount)
        {
            return false;
        }

        if (request.OddCount is not null && metrics.OddCountFor(request.NumbersPerGame) != request.OddCount)
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

    // Valida limites basicos aceitos pela cartela da modalidade.
    private static void ValidateRequest(BoardSpec spec, CoreGameRequest request)
    {
        if (request.GameCount is < 1 or > 100)
        {
            throw new ArgumentException("A quantidade de jogos deve estar entre 1 e 100.", nameof(request));
        }

        if (request.NumbersPerGame < spec.MinNumbersPerGame || request.NumbersPerGame > spec.MaxNumbersPerGame)
        {
            throw new ArgumentException(
                $"Cada jogo deve ter entre {spec.MinNumbersPerGame} e {spec.MaxNumbersPerGame} dezenas.",
                nameof(request));
        }

        if (request.EvenCount is not null && (request.EvenCount < 0 || request.EvenCount > request.NumbersPerGame))
        {
            throw new ArgumentException("A quantidade de pares e invalida.", nameof(request));
        }

        if (request.OddCount is not null && (request.OddCount < 0 || request.OddCount > request.NumbersPerGame))
        {
            throw new ArgumentException("A quantidade de impares e invalida.", nameof(request));
        }

        if (request.EvenCount is not null && request.OddCount is not null &&
            request.EvenCount + request.OddCount != request.NumbersPerGame)
        {
            throw new ArgumentException("A soma de pares e impares deve bater com o tamanho do jogo.", nameof(request));
        }

        if (request.SumRanges.Any(range => range.MinimumSum > range.MaximumSum))
        {
            throw new ArgumentException("Cada faixa de soma deve ter minimo menor ou igual ao maximo.", nameof(request));
        }
    }

    // Normaliza dezenas texto para inteiros validando o intervalo oficial da cartela.
    private static HashSet<int> NormalizeNumbers(BoardSpec spec, IReadOnlyCollection<string> numbers)
    {
        return numbers
            .Select(number =>
            {
                if (!int.TryParse(number.Trim(), out var parsed) || parsed < spec.FirstNumber || parsed > spec.LastNumber)
                {
                    throw new ArgumentException($"A dezena deve estar entre {spec.FirstNumber:00} e {spec.LastNumber:00}.", nameof(numbers));
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
        int MaxPerRowColumn)
    {
        // Impares deduzidos a partir dos pares e do tamanho do jogo.
        public int OddCountFor(int numbersPerGame) => numbersPerGame - EvenCount;
    }
}

// Descreve a cartela de uma modalidade para o nucleo de geracao.
public sealed record BoardSpec
{
    // Quantidade de casas da cartela (25 na Lotofacil, 100 na Lotomania).
    public required int BoardSize { get; init; }

    // Primeira dezena da cartela: 1 na maioria das modalidades e 0 na Lotomania (00-99).
    public int FirstNumber { get; init; } = 1;

    // Ultima dezena valida da cartela, deduzida a partir da primeira dezena e do tamanho.
    public int LastNumber => FirstNumber + BoardSize - 1;

    // Numero de colunas do volante; as linhas sao deduzidas por BoardSize / Columns.
    public required int Columns { get; init; }

    public required int MinNumbersPerGame { get; init; }

    public required int MaxNumbersPerGame { get; init; }

    public required IReadOnlySet<int> PrimeNumbers { get; init; }

    // Regra de miolo (linha, coluna) usada para calcular a moldura; null desativa o conceito de moldura.
    public Func<int, int, bool>? IsCenterCell { get; init; }
}

// Requisicao neutra de geracao consumida pelo nucleo; dezenas em texto no formato "01".
public sealed record CoreGameRequest
{
    public required int GameCount { get; init; }

    public required int NumbersPerGame { get; init; }

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

// Jogo gerado pelo nucleo com as dezenas em inteiros e as metricas calculadas.
public sealed record CoreGame(
    IReadOnlyList<int> Numbers,
    int EvenCount,
    int OddCount,
    int NumbersSum,
    int RepeatedFromPreviousCount,
    int PrimeCount,
    int BorderCount,
    int LongestSequence);

public sealed record CoreGameGenerationResult(
    IReadOnlyList<CoreGame> Games,
    int AttemptCount);
