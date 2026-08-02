namespace LotoAnalytics.Api.Features.GameGeneration;

public static class MaisMilionariaGameGenerator
{
    // Cartela principal oficial da +Milionaria: 50 dezenas, grade 10x5, primos ate 50 e sem moldura.
    public static readonly BoardSpec Board = new()
    {
        BoardSize = 50,
        Columns = 10,
        MinNumbersPerGame = 6,
        MaxNumbersPerGame = 12,
        PrimeNumbers = new HashSet<int> { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47 },
        IsCenterCell = null
    };

    // Faixa oficial dos trevos da +Milionaria (1 a 6) e minimo de dois trevos por aposta.
    public const int FirstTrevo = 1;
    public const int LastTrevo = 6;
    public const int MinTrevosPerGame = 2;
    public const int MaxTrevosPerGame = 6;

    // Gera jogos unicos da +Milionaria: sorteia as dezenas principais com filtros e, para cada jogo,
    // um conjunto independente de trevos (nenhum filtro estatistico se aplica aos trevos).
    public static MaisMilionariaGameGenerationResult Generate(MaisMilionariaGameGenerationRequest request, Random? random = null)
    {
        ValidateTrevos(request.TrevosPerGame);
        random ??= Random.Shared;

        var coreRequest = new CoreGameRequest
        {
            GameCount = request.GameCount,
            NumbersPerGame = request.NumbersPerGame,
            RequiredNumbers = request.RequiredNumbers,
            ExcludedNumbers = request.ExcludedNumbers,
            PreviousNumbers = request.PreviousNumbers,
            EvenCount = request.EvenCount,
            OddCount = request.OddCount,
            MinimumSum = request.MinimumSum,
            MaximumSum = request.MaximumSum,
            SumRanges = request.SumRanges,
            MinimumRepeated = request.MinimumRepeated,
            MaximumRepeated = request.MaximumRepeated,
            MinimumPrimes = request.MinimumPrimes,
            MaximumPrimes = request.MaximumPrimes,
            MinimumPerRowColumn = request.MinimumPerRowColumn,
            MaximumPerRowColumn = request.MaximumPerRowColumn,
            MaximumSequence = request.MaximumSequence,
            ForbiddenGameKeys = request.ForbiddenGameKeys
        };

        var result = GameGeneratorCore.Generate(Board, coreRequest, random);

        var games = result.Games
            .Select(game => new GeneratedMaisMilionariaGame(
                Numbers: game.Numbers.Select(number => number.ToString("00")).ToArray(),
                Trevos: DrawTrevos(request.TrevosPerGame, random),
                EvenCount: game.EvenCount,
                OddCount: game.OddCount,
                NumbersSum: game.NumbersSum,
                RepeatedFromPreviousCount: game.RepeatedFromPreviousCount,
                PrimeCount: game.PrimeCount,
                LongestSequence: game.LongestSequence))
            .ToArray();

        return new MaisMilionariaGameGenerationResult(games, result.AttemptCount);
    }

    // Sorteia, sem repeticao, a quantidade pedida de trevos entre 01 e 06 usando embaralhamento parcial.
    private static IReadOnlyList<string> DrawTrevos(int trevosPerGame, Random random)
    {
        var pool = Enumerable.Range(FirstTrevo, LastTrevo - FirstTrevo + 1).ToArray();
        for (var index = pool.Length - 1; index > 0; index--)
        {
            var swap = random.Next(index + 1);
            (pool[index], pool[swap]) = (pool[swap], pool[index]);
        }

        return pool
            .Take(trevosPerGame)
            .Order()
            .Select(trevo => trevo.ToString("00"))
            .ToArray();
    }

    // Garante que a quantidade de trevos respeita o minimo e o maximo oficiais da modalidade.
    private static void ValidateTrevos(int trevosPerGame)
    {
        if (trevosPerGame < MinTrevosPerGame || trevosPerGame > MaxTrevosPerGame)
        {
            throw new ArgumentException(
                $"Cada jogo deve ter entre {MinTrevosPerGame} e {MaxTrevosPerGame} trevos.",
                nameof(trevosPerGame));
        }
    }

    // Monta a chave canonica de um jogo no formato usado para comparar com sorteios da base.
    public static string FormatGameKey(IReadOnlyCollection<int> numbers) => GameGeneratorCore.FormatGameKey(numbers);
}

public sealed record MaisMilionariaGameGenerationRequest
{
    public required int GameCount { get; init; }

    public int NumbersPerGame { get; init; } = 6;

    public int TrevosPerGame { get; init; } = 2;

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

    public int? MinimumPerRowColumn { get; init; }

    public int? MaximumPerRowColumn { get; init; }

    public int? MaximumSequence { get; init; }

    public IReadOnlySet<string>? ForbiddenGameKeys { get; init; }
}

public sealed record MaisMilionariaGameGenerationResult(
    IReadOnlyList<GeneratedMaisMilionariaGame> Games,
    int AttemptCount);

public sealed record GeneratedMaisMilionariaGame(
    IReadOnlyList<string> Numbers,
    IReadOnlyList<string> Trevos,
    int EvenCount,
    int OddCount,
    int NumbersSum,
    int RepeatedFromPreviousCount,
    int PrimeCount,
    int LongestSequence);
