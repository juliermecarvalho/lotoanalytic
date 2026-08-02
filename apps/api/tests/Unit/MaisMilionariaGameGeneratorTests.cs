using LotoAnalytics.Api.Features.GameGeneration;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class MaisMilionariaGameGeneratorTests
{
    private static readonly string[] PreviousDraw = ["03", "17", "22", "31", "44", "48"];

    [Fact]
    public void GenerateReturnsUniqueGamesWithinTheOfficialBoardThatMatchRequestedFilters()
    {
        var request = new MaisMilionariaGameGenerationRequest
        {
            GameCount = 3,
            NumbersPerGame = 6,
            TrevosPerGame = 2,
            RequiredNumbers = ["01", "02"],
            ExcludedNumbers = ["50"],
            EvenCount = 3,
            OddCount = 3
        };

        var result = MaisMilionariaGameGenerator.Generate(request, new Random(42));

        result.Games.Count.ShouldBe(3);
        result.AttemptCount.ShouldBeGreaterThan(0);
        result.Games.DistinctBy(game => string.Join(",", game.Numbers)).Count().ShouldBe(3);

        foreach (var game in result.Games)
        {
            game.Numbers.Count.ShouldBe(6);
            game.Numbers.ShouldContain("01");
            game.Numbers.ShouldContain("02");
            game.Numbers.ShouldNotContain("50");
            game.Numbers.ShouldAllBe(number => int.Parse(number) >= 1 && int.Parse(number) <= 50);
            game.EvenCount.ShouldBe(3);
            game.OddCount.ShouldBe(3);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(6)]
    public void GenerateDrawsTheRequestedAmountOfDistinctTrevosWithinTheOfficialRange(int trevosPerGame)
    {
        var request = new MaisMilionariaGameGenerationRequest
        {
            GameCount = 5,
            NumbersPerGame = 6,
            TrevosPerGame = trevosPerGame
        };

        var result = MaisMilionariaGameGenerator.Generate(request, new Random(99));

        result.Games.Count.ShouldBe(5);
        foreach (var game in result.Games)
        {
            game.Trevos.Count.ShouldBe(trevosPerGame);
            game.Trevos.Distinct().Count().ShouldBe(trevosPerGame);
            game.Trevos.ShouldAllBe(trevo => int.Parse(trevo) >= 1 && int.Parse(trevo) <= 6);
            // Os trevos devem sair sempre ordenados de forma crescente.
            game.Trevos.Select(int.Parse).ShouldBe(game.Trevos.Select(int.Parse).OrderBy(value => value));
        }
    }

    [Fact]
    public void GenerateAppliesAllStatisticalFiltersFromTheStrategy()
    {
        var request = new MaisMilionariaGameGenerationRequest
        {
            GameCount = 4,
            NumbersPerGame = 6,
            TrevosPerGame = 2,
            PreviousNumbers = PreviousDraw,
            EvenCount = 3,
            SumRanges = [new SumRangeFilter(100, 200)],
            MinimumRepeated = 0,
            MaximumRepeated = 2,
            MinimumPrimes = 1,
            MaximumPrimes = 3,
            MaximumPerRowColumn = 2,
            MaximumSequence = 3
        };

        var result = MaisMilionariaGameGenerator.Generate(request, new Random(7));

        result.Games.Count.ShouldBe(4);

        foreach (var game in result.Games)
        {
            game.EvenCount.ShouldBe(3);
            game.NumbersSum.ShouldBeInRange(100, 200);
            game.RepeatedFromPreviousCount.ShouldBeInRange(0, 2);
            game.PrimeCount.ShouldBeInRange(1, 3);
            game.LongestSequence.ShouldBeLessThanOrEqualTo(3);
        }
    }

    [Fact]
    public void GenerateAcceptsUpToTwelveMainNumbersPerGame()
    {
        var request = new MaisMilionariaGameGenerationRequest
        {
            GameCount = 2,
            NumbersPerGame = 12,
            TrevosPerGame = 2
        };

        var result = MaisMilionariaGameGenerator.Generate(request, new Random(13));

        result.Games.Count.ShouldBe(2);
        result.Games.ShouldAllBe(game => game.Numbers.Count == 12);
    }

    [Fact]
    public void GenerateReturnsPartialResultWhenFiltersCannotBeSatisfied()
    {
        // A soma maxima de 6 dezenas da +Milionaria e 45+46+47+48+49+50 = 285; exigir 286 torna o filtro impossivel.
        var request = new MaisMilionariaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            TrevosPerGame = 2,
            MinimumSum = 286
        };

        var result = MaisMilionariaGameGenerator.Generate(request, new Random(3));

        result.Games.ShouldBeEmpty();
        result.AttemptCount.ShouldBe(250_000);
    }

    [Fact]
    public void GenerateRejectsRequiredNumbersAlsoMarkedAsExcluded()
    {
        var request = new MaisMilionariaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            TrevosPerGame = 2,
            RequiredNumbers = ["05"],
            ExcludedNumbers = ["05"]
        };

        Should.Throw<ArgumentException>(() => MaisMilionariaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsGamesSmallerThanTheMinimumBet()
    {
        var request = new MaisMilionariaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 5,
            TrevosPerGame = 2
        };

        Should.Throw<ArgumentException>(() => MaisMilionariaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsNumbersOutsideTheOfficialBoard()
    {
        var request = new MaisMilionariaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            TrevosPerGame = 2,
            RequiredNumbers = ["51"]
        };

        Should.Throw<ArgumentException>(() => MaisMilionariaGameGenerator.Generate(request));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    public void GenerateRejectsTrevoCountsOutsideTheOfficialRange(int trevosPerGame)
    {
        var request = new MaisMilionariaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            TrevosPerGame = trevosPerGame
        };

        Should.Throw<ArgumentException>(() => MaisMilionariaGameGenerator.Generate(request));
    }
}
