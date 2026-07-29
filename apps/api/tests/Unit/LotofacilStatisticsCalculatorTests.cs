using LotoAnalytics.Api.Features.Statistics;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class LotofacilStatisticsCalculatorTests
{
    [Fact]
    public void CalculateReturnsExpectedStatisticsForLotofacilNumbers()
    {
        var dezenas = new[]
        {
            "01", "02", "03", "04", "05",
            "06", "07", "08", "09", "10",
            "11", "13", "17", "19", "23"
        };
        var dezenasAnteriores = new[]
        {
            "01", "02", "04", "06", "08",
            "10", "12", "14", "16", "18",
            "20", "21", "22", "23", "24"
        };

        var resultado = LotofacilStatisticsCalculator.Calculate(dezenas, dezenasAnteriores);

        resultado.EvenCount.ShouldBe(5);
        resultado.OddCount.ShouldBe(10);
        resultado.NumbersSum.ShouldBe(138);
        resultado.RepeatedFromPreviousCount.ShouldBe(7);
        resultado.RepeatedFromPrevious.ShouldBe(["01", "02", "04", "06", "08", "10", "23"]);
        resultado.PrimeCount.ShouldBe(9);
        resultado.BorderCount.ShouldBe(9);
        resultado.CenterCount.ShouldBe(6);
        resultado.LongestSequence.ShouldBe(11);
        resultado.RowDistribution.ShouldBe([5, 5, 2, 2, 1]);
        resultado.ColumnDistribution.ShouldBe([3, 3, 4, 3, 2]);
    }
}
