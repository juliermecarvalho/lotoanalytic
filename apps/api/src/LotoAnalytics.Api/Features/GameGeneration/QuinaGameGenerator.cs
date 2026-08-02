namespace LotoAnalytics.Api.Features.GameGeneration;

public static class QuinaGameGenerator
{
    // Cartela oficial da Quina: 80 dezenas, grade 10x8, primos ate 80 e sem conceito de moldura.
    public static readonly BoardSpec Board = new()
    {
        BoardSize = 80,
        Columns = 10,
        MinNumbersPerGame = 5,
        MaxNumbersPerGame = 15,
        PrimeNumbers = new HashSet<int> { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79 },
        IsCenterCell = null
    };

    // Gera jogos unicos da Quina por amostragem aleatoria aplicando os filtros estatisticos.
    public static QuinaGameGenerationResult Generate(QuinaGameGenerationRequest request, Random? random = null)
    {
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
            .Select(game => new GeneratedQuinaGame(
                Numbers: game.Numbers.Select(number => number.ToString("00")).ToArray(),
                EvenCount: game.EvenCount,
                OddCount: game.OddCount,
                NumbersSum: game.NumbersSum,
                RepeatedFromPreviousCount: game.RepeatedFromPreviousCount,
                PrimeCount: game.PrimeCount,
                LongestSequence: game.LongestSequence))
            .ToArray();

        return new QuinaGameGenerationResult(games, result.AttemptCount);
    }

    // Monta a chave canonica de um jogo no formato usado para comparar com sorteios da base.
    public static string FormatGameKey(IReadOnlyCollection<int> numbers) => GameGeneratorCore.FormatGameKey(numbers);
}

public sealed record QuinaGameGenerationRequest
{
    public required int GameCount { get; init; }

    public int NumbersPerGame { get; init; } = 5;

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

public sealed record QuinaGameGenerationResult(
    IReadOnlyList<GeneratedQuinaGame> Games,
    int AttemptCount);

public sealed record GeneratedQuinaGame(
    IReadOnlyList<string> Numbers,
    int EvenCount,
    int OddCount,
    int NumbersSum,
    int RepeatedFromPreviousCount,
    int PrimeCount,
    int LongestSequence);
