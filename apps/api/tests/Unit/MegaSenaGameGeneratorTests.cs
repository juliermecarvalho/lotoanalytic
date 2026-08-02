using LotoAnalytics.Api.Features.GameGeneration;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class MegaSenaGameGeneratorTests
{
    private static readonly string[] PreviousDraw = ["04", "10", "20", "33", "41", "52"];

    [Fact]
    public void GenerateReturnsUniqueGamesWithinTheOfficialBoardThatMatchRequestedFilters()
    {
        var request = new MegaSenaGameGenerationRequest
        {
            GameCount = 3,
            NumbersPerGame = 6,
            RequiredNumbers = ["01", "02"],
            ExcludedNumbers = ["60"],
            EvenCount = 3,
            OddCount = 3
        };

        var result = MegaSenaGameGenerator.Generate(request, new Random(42));

        result.Games.Count.ShouldBe(3);
        result.AttemptCount.ShouldBeGreaterThan(0);
        result.Games.DistinctBy(game => string.Join(",", game.Numbers)).Count().ShouldBe(3);

        foreach (var game in result.Games)
        {
            game.Numbers.Count.ShouldBe(6);
            game.Numbers.ShouldContain("01");
            game.Numbers.ShouldContain("02");
            game.Numbers.ShouldNotContain("60");
            game.Numbers.ShouldAllBe(number => int.Parse(number) >= 1 && int.Parse(number) <= 60);
            game.EvenCount.ShouldBe(3);
            game.OddCount.ShouldBe(3);
        }
    }

    [Fact]
    public void GenerateAppliesAllStatisticalFiltersFromTheStrategy()
    {
        var request = new MegaSenaGameGenerationRequest
        {
            GameCount = 4,
            NumbersPerGame = 6,
            PreviousNumbers = PreviousDraw,
            EvenCount = 3,
            SumRanges = [new SumRangeFilter(120, 220)],
            MinimumRepeated = 0,
            MaximumRepeated = 2,
            MinimumPrimes = 1,
            MaximumPrimes = 3,
            MaximumPerRowColumn = 2,
            MaximumSequence = 3
        };

        var result = MegaSenaGameGenerator.Generate(request, new Random(7));

        result.Games.Count.ShouldBe(4);

        foreach (var game in result.Games)
        {
            game.EvenCount.ShouldBe(3);
            game.NumbersSum.ShouldBeInRange(120, 220);
            game.RepeatedFromPreviousCount.ShouldBeInRange(0, 2);
            game.PrimeCount.ShouldBeInRange(1, 3);
            game.LongestSequence.ShouldBeLessThanOrEqualTo(3);
        }
    }

    [Fact]
    public void GenerateAcceptsSumsInAnyConfiguredSumRange()
    {
        var request = new MegaSenaGameGenerationRequest
        {
            GameCount = 5,
            NumbersPerGame = 6,
            SumRanges = [new SumRangeFilter(80, 100), new SumRangeFilter(250, 270)]
        };

        var result = MegaSenaGameGenerator.Generate(request, new Random(11));

        result.Games.Count.ShouldBe(5);
        foreach (var game in result.Games)
        {
            (game.NumbersSum is >= 80 and <= 100 or >= 250 and <= 270).ShouldBeTrue(
                $"Soma {game.NumbersSum} fora das faixas configuradas.");
        }
    }

    [Fact]
    public void GenerateReturnsPartialResultWhenFiltersCannotBeSatisfied()
    {
        // A soma maxima de 6 dezenas da Mega-Sena e 55+56+57+58+59+60 = 345; exigir 346 torna o filtro impossivel.
        var request = new MegaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            MinimumSum = 346
        };

        var result = MegaSenaGameGenerator.Generate(request, new Random(3));

        result.Games.ShouldBeEmpty();
        result.AttemptCount.ShouldBe(250_000);
    }

    [Fact]
    public void GenerateSkipsGamesAlreadyDrawnInTheHistoricalBase()
    {
        var onlyPossibleGame = Enumerable.Range(1, 6).ToArray();

        // Com 6 dezenas obrigatorias existe um unico jogo possivel; marca-lo como sorteado zera a geracao.
        var request = new MegaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            RequiredNumbers = onlyPossibleGame.Select(number => number.ToString("00")).ToArray(),
            ForbiddenGameKeys = new HashSet<string> { MegaSenaGameGenerator.FormatGameKey(onlyPossibleGame) }
        };

        var result = MegaSenaGameGenerator.Generate(request, new Random(5));

        result.Games.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRejectsRequiredNumbersAlsoMarkedAsExcluded()
    {
        var request = new MegaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            RequiredNumbers = ["05"],
            ExcludedNumbers = ["05"]
        };

        Should.Throw<ArgumentException>(() => MegaSenaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsGamesSmallerThanTheMinimumBet()
    {
        var request = new MegaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 5
        };

        Should.Throw<ArgumentException>(() => MegaSenaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsNumbersOutsideTheOfficialBoard()
    {
        var request = new MegaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            RequiredNumbers = ["61"]
        };

        Should.Throw<ArgumentException>(() => MegaSenaGameGenerator.Generate(request));
    }
}
