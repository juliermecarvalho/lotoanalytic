using LotoAnalytics.Api.Features.GameGeneration;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class DuplaSenaGameGeneratorTests
{
    private static readonly string[] PreviousDraw = ["03", "17", "22", "31", "44", "48"];

    [Fact]
    public void GenerateReturnsUniqueGamesWithinTheOfficialBoardThatMatchRequestedFilters()
    {
        var request = new DuplaSenaGameGenerationRequest
        {
            GameCount = 3,
            NumbersPerGame = 6,
            RequiredNumbers = ["01", "02"],
            ExcludedNumbers = ["50"],
            EvenCount = 3,
            OddCount = 3
        };

        var result = DuplaSenaGameGenerator.Generate(request, new Random(42));

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

    [Fact]
    public void GenerateAppliesAllStatisticalFiltersFromTheStrategy()
    {
        var request = new DuplaSenaGameGenerationRequest
        {
            GameCount = 4,
            NumbersPerGame = 6,
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

        var result = DuplaSenaGameGenerator.Generate(request, new Random(7));

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
    public void GenerateAcceptsSumsInAnyConfiguredSumRange()
    {
        var request = new DuplaSenaGameGenerationRequest
        {
            GameCount = 5,
            NumbersPerGame = 6,
            SumRanges = [new SumRangeFilter(60, 90), new SumRangeFilter(230, 260)]
        };

        var result = DuplaSenaGameGenerator.Generate(request, new Random(11));

        result.Games.Count.ShouldBe(5);
        foreach (var game in result.Games)
        {
            (game.NumbersSum is >= 60 and <= 90 or >= 230 and <= 260).ShouldBeTrue(
                $"Soma {game.NumbersSum} fora das faixas configuradas.");
        }
    }

    [Fact]
    public void GenerateReturnsPartialResultWhenFiltersCannotBeSatisfied()
    {
        // A soma maxima de 6 dezenas da Dupla Sena e 45+46+47+48+49+50 = 285; exigir 286 torna o filtro impossivel.
        var request = new DuplaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            MinimumSum = 286
        };

        var result = DuplaSenaGameGenerator.Generate(request, new Random(3));

        result.Games.ShouldBeEmpty();
        result.AttemptCount.ShouldBe(250_000);
    }

    [Fact]
    public void GenerateSkipsGamesAlreadyDrawnInTheHistoricalBase()
    {
        var onlyPossibleGame = Enumerable.Range(1, 6).ToArray();

        // Com 6 dezenas obrigatorias existe um unico jogo possivel; marca-lo como sorteado zera a geracao.
        var request = new DuplaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            RequiredNumbers = onlyPossibleGame.Select(number => number.ToString("00")).ToArray(),
            ForbiddenGameKeys = new HashSet<string> { DuplaSenaGameGenerator.FormatGameKey(onlyPossibleGame) }
        };

        var result = DuplaSenaGameGenerator.Generate(request, new Random(5));

        result.Games.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRejectsRequiredNumbersAlsoMarkedAsExcluded()
    {
        var request = new DuplaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            RequiredNumbers = ["05"],
            ExcludedNumbers = ["05"]
        };

        Should.Throw<ArgumentException>(() => DuplaSenaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsGamesSmallerThanTheMinimumBet()
    {
        var request = new DuplaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 5
        };

        Should.Throw<ArgumentException>(() => DuplaSenaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsNumbersOutsideTheOfficialBoard()
    {
        var request = new DuplaSenaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 6,
            RequiredNumbers = ["51"]
        };

        Should.Throw<ArgumentException>(() => DuplaSenaGameGenerator.Generate(request));
    }
}
