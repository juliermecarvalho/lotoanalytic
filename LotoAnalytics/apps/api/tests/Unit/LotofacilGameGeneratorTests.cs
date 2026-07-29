using LotoAnalytics.Api.Features.GameGeneration;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class LotofacilGameGeneratorTests
{
    private static readonly string[] PreviousDraw =
        ["01", "02", "03", "05", "07", "09", "11", "13", "14", "17", "19", "20", "22", "24", "25"];

    [Fact]
    public void GenerateReturnsUniqueGamesThatMatchRequestedFilters()
    {
        var request = new LotofacilGameGenerationRequest
        {
            GameCount = 3,
            NumbersPerGame = 15,
            RequiredNumbers = ["01", "02"],
            ExcludedNumbers = ["25"],
            EvenCount = 7,
            OddCount = 8,
            MinimumSum = 120,
            MaximumSum = 210
        };

        var result = LotofacilGameGenerator.Generate(request, new Random(42));

        result.Games.Count.ShouldBe(3);
        result.AttemptCount.ShouldBeGreaterThan(0);
        result.Games.DistinctBy(game => string.Join(",", game.Numbers)).Count().ShouldBe(3);

        foreach (var game in result.Games)
        {
            game.Numbers.Count.ShouldBe(15);
            game.Numbers.ShouldContain("01");
            game.Numbers.ShouldContain("02");
            game.Numbers.ShouldNotContain("25");
            game.EvenCount.ShouldBe(7);
            game.OddCount.ShouldBe(8);
            game.NumbersSum.ShouldBeInRange(120, 210);
        }
    }

    [Fact]
    public void GenerateAppliesAllStatisticalFiltersFromTheStrategy()
    {
        var request = new LotofacilGameGenerationRequest
        {
            GameCount = 4,
            NumbersPerGame = 15,
            PreviousNumbers = PreviousDraw,
            EvenCount = 7,
            SumRanges = [new SumRangeFilter(185, 210)],
            MinimumRepeated = 9,
            MaximumRepeated = 9,
            MinimumPrimes = 5,
            MaximumPrimes = 5,
            MinimumBorder = 9,
            MaximumBorder = 9,
            MinimumPerRowColumn = 2,
            MaximumPerRowColumn = 4,
            MaximumSequence = 5
        };

        var result = LotofacilGameGenerator.Generate(request, new Random(7));

        result.Games.Count.ShouldBe(4);

        foreach (var game in result.Games)
        {
            game.EvenCount.ShouldBe(7);
            game.NumbersSum.ShouldBeInRange(185, 210);
            game.RepeatedFromPreviousCount.ShouldBe(9);
            game.PrimeCount.ShouldBe(5);
            game.BorderCount.ShouldBe(9);
            game.LongestSequence.ShouldBeLessThanOrEqualTo(5);
        }
    }

    [Fact]
    public void GenerateAcceptsSumsInAnyConfiguredSumRange()
    {
        var request = new LotofacilGameGenerationRequest
        {
            GameCount = 5,
            NumbersPerGame = 15,
            SumRanges = [new SumRangeFilter(180, 184), new SumRangeFilter(211, 212)]
        };

        var result = LotofacilGameGenerator.Generate(request, new Random(11));

        result.Games.Count.ShouldBe(5);
        foreach (var game in result.Games)
        {
            (game.NumbersSum is >= 180 and <= 184 or >= 211 and <= 212).ShouldBeTrue(
                $"Soma {game.NumbersSum} fora das faixas configuradas.");
        }
    }

    [Fact]
    public void GenerateReturnsPartialResultWhenFiltersCannotBeSatisfied()
    {
        // Excluir dezenas primas deixa no maximo 7 primos disponiveis, tornando o filtro impossivel.
        var request = new LotofacilGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 15,
            ExcludedNumbers = ["02", "03"],
            MinimumPrimes = 8
        };

        var result = LotofacilGameGenerator.Generate(request, new Random(3));

        result.Games.ShouldBeEmpty();
        result.AttemptCount.ShouldBe(250_000);
    }

    [Fact]
    public void GenerateSkipsGamesAlreadyDrawnInTheHistoricalBase()
    {
        var onlyPossibleGame = Enumerable.Range(11, 15).ToArray();

        // Com 15 dezenas obrigatorias existe um unico jogo possivel; marca-lo como sorteado zera a geracao.
        var request = new LotofacilGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 15,
            RequiredNumbers = Enumerable.Range(11, 15).Select(number => number.ToString("00")).ToArray(),
            ForbiddenGameKeys = new HashSet<string> { LotofacilGameGenerator.FormatGameKey(onlyPossibleGame) }
        };

        var result = LotofacilGameGenerator.Generate(request, new Random(5));

        result.Games.ShouldBeEmpty();
    }

    [Fact]
    public void GenerateRejectsRequiredNumbersAlsoMarkedAsExcluded()
    {
        var request = new LotofacilGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 15,
            RequiredNumbers = ["05"],
            ExcludedNumbers = ["05"]
        };

        Should.Throw<ArgumentException>(() => LotofacilGameGenerator.Generate(request));
    }

    [Fact]
    public void GenerateRejectsInvalidSumRanges()
    {
        var request = new LotofacilGameGenerationRequest
        {
            GameCount = 1,
            NumbersPerGame = 15,
            SumRanges = [new SumRangeFilter(210, 185)]
        };

        Should.Throw<ArgumentException>(() => LotofacilGameGenerator.Generate(request));
    }
}
