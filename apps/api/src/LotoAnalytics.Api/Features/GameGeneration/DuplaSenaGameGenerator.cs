namespace LotoAnalytics.Api.Features.GameGeneration;

public static class DuplaSenaGameGenerator
{
    // Cartela oficial da Dupla Sena: 50 dezenas, grade 10x5, primos ate 50 e sem conceito de moldura.
    public static readonly BoardSpec Board = new()
    {
        BoardSize = 50,
        Columns = 10,
        MinNumbersPerGame = 6,
        MaxNumbersPerGame = 15,
        PrimeNumbers = new HashSet<int> { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47 },
        IsCenterCell = null
    };

    // Gera jogos unicos da Dupla Sena por amostragem aleatoria aplicando os filtros estatisticos.
    public static DuplaSenaGameGenerationResult Generate(DuplaSenaGameGenerationRequest request, Random? random = null)
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
            .Select(game => new GeneratedDuplaSenaGame(
                Numbers: game.Numbers.Select(number => number.ToString("00")).ToArray(),
                EvenCount: game.EvenCount,
                OddCount: game.OddCount,
                NumbersSum: game.NumbersSum,
                RepeatedFromPreviousCount: game.RepeatedFromPreviousCount,
                PrimeCount: game.PrimeCount,
                LongestSequence: game.LongestSequence))
            .ToArray();

        return new DuplaSenaGameGenerationResult(games, result.AttemptCount);
    }

    // Monta a chave canonica de um jogo no formato usado para comparar com sorteios da base.
    public static string FormatGameKey(IReadOnlyCollection<int> numbers) => GameGeneratorCore.FormatGameKey(numbers);
}

public sealed record DuplaSenaGameGenerationRequest
{
    public required int GameCount { get; init; }

    public int NumbersPerGame { get; init; } = 6;

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

public sealed record DuplaSenaGameGenerationResult(
    IReadOnlyList<GeneratedDuplaSenaGame> Games,
    int AttemptCount);

public sealed record GeneratedDuplaSenaGame(
    IReadOnlyList<string> Numbers,
    int EvenCount,
    int OddCount,
    int NumbersSum,
    int RepeatedFromPreviousCount,
    int PrimeCount,
    int LongestSequence);
