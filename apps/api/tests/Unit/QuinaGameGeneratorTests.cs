using LotoAnalytics.Api.Features.GameGeneration;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class QuinaGameGeneratorTests
{
    private static readonly string[] PreviousDraw = ["04", "19", "28", "51", "77"];

    [Fact]
    public void GenerateReturnsUniqueGamesWithinTheOfficialBoardThatMatchRequestedFilters()
    {
        var request = new QuinaGameGenerationRequest
        {
            GameCount = 3,
            NumbersPerGame = 5,
            RequiredNumbers = ["01", "02"],
            ExcludedNumbers = ["80"],
            EvenCount = 2,
            OddCount = 3
        };

        var result = QuinaGameGenerator.Generate(request, new Random(42));

        result.Games.Count.ShouldBe(3);
        result.AttemptCount.ShouldBeGreaterThan(0);
        result.Games.DistinctBy(game => string.Join(",", game.Numbers)).Count().ShouldBe(3);

        foreach (var game in result.Games)
        {
            game.Numbers.Count.ShouldBe(5);
            game.Numbers.ShouldContain("01");
            game.Numbers.ShouldContain("02");
            game.Numbers.ShouldNotContain("80");
            game.Numbers.ShouldAllBe(number => int.Parse(number) >= 1 && int.Parse(number) <= 80);
            game.EvenCount.ShouldBe(2);
            game.OddCount.ShouldBe(3);
        }
    }

    [Fact]
    public void GenerateAppliesAllStatisticalFiltersFromTheStrategy()
    {
        var request = new QuinaGameGenerationRequest
        {
            GameCount = 4,
            NumbersPerGame = 5,
            PreviousNumbers = PreviousDraw,
            EvenCount = 2,
            SumRanges = [new SumRangeFilter(120, 280)],
            MinimumRepeated = 0,
            MaximumRepeated = 2,
            MinimumPrimes = 1,
            MaximumPrimes = 3,
            MaximumPerRowColumn = 2,
            MaximumSequence = 3
        };

        var result = QuinaGameGenerator.Generate(request, new Random(7));

        result.Games.Count.ShouldBe(4);

        foreach (var game in result.Games)
        {
            game.EvenCount.ShouldBe(2);
            game.NumbersSum.ShouldBeInRange(120, 280);
            game.RepeatedFromPreviousCount.ShouldBeInRange(0, 2);
            game.PrimeCount.ShouldBeInRange(1, 3);
            game.LongestSequence.ShouldBeLessThanOrEqualTo(3);
        }
    }

    [Fact]
    public void GenerateAcceptsSumsInAnyConfiguredSumRange()
    {
        var request = new QuinaGameGenerationRequest
        {
            GameCount = 5,
            NumbersPerGame = 5,
            SumRanges = [new SumRangeFilter(80, 110), new SumRangeFilter(300, 330)]
        };

        var result = QuinaGameGenerator.Generate(request, new Random(11));

        result.Games.Count.ShouldBe(5);
        foreach (var game in result.Games)
        {
            (game.NumbersSum is >= 80 and <= 110 or >= 300 and <= 330).ShouldBeTrue(
                $"Soma {game.NumbersSum} fora das faixas configuradas.");
        }
    }

    [Fact]
    public void GenerateReturnsPartialResultWhenFiltersCannotBeSatisfied()
    {
        // A soma maxima de 5 dezenas da Quina e 76+77+78+79+80 = 390; exigir 391 torna o filtro impossivel.
        var request = new QuinaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 5,
            MinimumSum = 391
        };

        var result = QuinaGameGenerator.Generate(request, new Random(3));

        result.Games.ShouldBeEmpty();
        result.AttemptCount.ShouldBe(250_000);
    }

    [Fact]
    public void GenerateSkipsGamesAlreadyDrawnInTheHistoricalBase()
    {
        var onlyPossibleGame = Enumerable.Range(1, 5).ToArray();

        // Com 5 dezenas obrigatorias existe um unico jogo possivel; marca-lo como sorteado zera a geracao.
        var request = new QuinaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 5,
            RequiredNumbers = onlyPossibleGame.Select(number => number.ToString("00")).ToArray(),
            ForbiddenGameKeys = new HashSet<string> { QuinaGameGenerator.FormatGameKey(onlyPossibleGame) }
        };

        var result = QuinaGameGenerator.Generate(request, new Random(5));

        result.Games.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRejectsRequiredNumbersAlsoMarkedAsExcluded()
    {
        var request = new QuinaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 5,
            RequiredNumbers = ["05"],
            ExcludedNumbers = ["05"]
        };

        Should.Throw<ArgumentException>(() => QuinaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsGamesSmallerThanTheMinimumBet()
    {
        var request = new QuinaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 4
        };

        Should.Throw<ArgumentException>(() => QuinaGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsNumbersOutsideTheOfficialBoard()
    {
        var request = new QuinaGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 5,
            RequiredNumbers = ["81"]
        };

        Should.Throw<ArgumentException>(() => QuinaGameGenerator.Generate(request));
    }
}
