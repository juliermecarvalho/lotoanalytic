using LotoAnalytics.Api.Features.GameGeneration;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class LotomaniaGameGeneratorTests
{
    private static readonly string[] PreviousDraw =
    [
        "00", "01", "09", "10", "12", "13", "18", "25", "35", "37",
        "38", "46", "54", "72", "79", "80", "82", "83", "84", "91"
    ];

    [Fact]
    public void GenerateReturnsUniqueBetsOfFiftyNumbersWithinTheZeroBasedBoard()
    {
        var request = new LotomaniaGameGenerationRequest
        {
            GameCount = 3,
            NumbersPerGame = 50,
            // "00" so e valido porque a cartela da Lotomania comeca no zero (FirstNumber = 0).
            RequiredNumbers = ["00", "01"],
            ExcludedNumbers = ["99"],
            EvenCount = 25,
            OddCount = 25
        };

        var result = LotomaniaGameGenerator.Generate(request, new Random(42));

        result.Games.Count.ShouldBe(3);
        result.AttemptCount.ShouldBeGreaterThan(0);
        result.Games.DistinctBy(game => string.Join(",", game.Numbers)).Count().ShouldBe(3);

        foreach (var game in result.Games)
        {
            game.Numbers.Count.ShouldBe(50);
            game.Numbers.ShouldContain("00");
            game.Numbers.ShouldContain("01");
            game.Numbers.ShouldNotContain("99");
            game.Numbers.ShouldAllBe(number => int.Parse(number) >= 0 && int.Parse(number) <= 99);
            game.EvenCount.ShouldBe(25);
            game.OddCount.ShouldBe(25);
        }
    }

    [Fact]
    public void GenerateAppliesAllStatisticalFiltersFromTheStrategy()
    {
        var request = new LotomaniaGameGenerationRequest
        {
            GameCount = 4,
            NumbersPerGame = 50,
            PreviousNumbers = PreviousDraw,
            EvenCount = 25,
            SumRanges = [new SumRangeFilter(2200, 2750)],
            MinimumRepeated = 5,
            MaximumRepeated = 15,
            MinimumPrimes = 10,
            MaximumPrimes = 15
        };

        var result = LotomaniaGameGenerator.Generate(request, new Random(7));

        result.Games.Count.ShouldBe(4);

        foreach (var game in result.Games)
        {
            game.EvenCount.ShouldBe(25);
            game.NumbersSum.ShouldBeInRange(2200, 2750);
            game.RepeatedFromPreviousCount.ShouldBeInRange(5, 15);
            game.PrimeCount.ShouldBeInRange(10, 15);
        }
    }

    [Fact]
    public void GenerateAcceptsSumsInAnyConfiguredSumRange()
    {
        var request = new LotomaniaGameGenerationRequest
        {
            GameCount = 5,
            NumbersPerGame = 50,
            SumRanges = [new SumRangeFilter(2100, 2300), new SumRangeFilter(2650, 2850)]
        };

        var result = LotomaniaGameGenerator.Generate(request, new Random(11));

        result.Games.Count.ShouldBe(5);
        foreach (var game in result.Games)
        {
            (game.NumbersSum is >= 2100 and <= 2300 or >= 2650 and <= 2850).ShouldBeTrue(
                $"Soma {game.NumbersSum} fora das faixas configuradas.");
        }
    }

    [Fact]
    public void GenerateReturnsPartialResultWhenFiltersCannotBeSatisfied()
    {
        // A soma maxima de 50 dezenas da Lotomania e 50+51+...+99 = 3725; exigir 3726 torna o filtro impossivel.
        var request = new LotomaniaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 50,
            MinimumSum = 3726
        };

        var result = LotomaniaGameGenerator.Generate(request, new Random(3));

        result.Games.ShouldBeEmpty();
        result.AttemptCount.ShouldBe(250_000);
    }

    [Fact]
    public void GenerateRejectsRequiredNumbersAlsoMarkedAsExcluded()
    {
        var request = new LotomaniaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 50,
            RequiredNumbers = ["05"],
            ExcludedNumbers = ["05"]
        };

        Should.Throw<ArgumentException>(() => LotomaniaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsBetsDifferentFromFiftyNumbers()
    {
        var request = new LotomaniaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 40
        };

        Should.Throw<ArgumentException>(() => LotomaniaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsNumbersOutsideTheOfficialBoard()
    {
        var request = new LotomaniaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 50,
            RequiredNumbers = ["100"]
        };

        Should.Throw<ArgumentException>(() => LotomaniaGameGenerator.Generate(request));
    }
}
