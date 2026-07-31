using LotoAnalytics.Api.Features.Dashboard;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class LotofacilDashboardAggregatorTests
{
    // Mesmo conjunto de sorteios dos demais testes, com metricas calculadas manualmente.
    private static readonly DashboardDraw[] Draws =
    [
        new(1, new DateOnly(2026, 7, 20), [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]),
        new(2, new DateOnly(2026, 7, 21), [1, 2, 3, 5, 7, 9, 11, 13, 14, 17, 19, 20, 22, 24, 25]),
        new(3, new DateOnly(2026, 7, 22), [2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 21, 22, 23, 24, 25])
    ];

    [Fact]
    public void AggregateComputesFrequenciesDelaysAndSummaries()
    {
        var snapshot = LotofacilDashboardAggregator.Aggregate(Draws);

        snapshot.TotalContests.ShouldBe(3);
        snapshot.Frequencies.Count.ShouldBe(25);

        // Dezena 2 saiu nos tres concursos: frequencia maxima e atraso zero.
        var number2 = snapshot.Frequencies.Single(item => item.Number == 2);
        number2.Count.ShouldBe(3);
        number2.Delay.ShouldBe(0);
        number2.LastContest.ShouldBe(3);

        // Dezena 1 saiu nos concursos 1 e 2, ficando de fora do concurso 3 (atraso 1).
        var number1 = snapshot.Frequencies.Single(item => item.Number == 1);
        number1.Count.ShouldBe(2);
        number1.Delay.ShouldBe(1);
        number1.LastContest.ShouldBe(2);

        // Soma media = (120 + 192 + 225) / 3.
        snapshot.Summary.AverageSum.ShouldBe(179.0);
        // Repeticao media = (9 + 6) / 2.
        snapshot.Summary.AverageRepetition.ShouldBe(7.5);
        // Nenhum sorteio se repetiu na base.
        snapshot.Summary.UniqueCombinationsPercentage.ShouldBe(100.0);
        // Apenas o concurso 2 (soma 192) esta na faixa preferencial 185-210.
        snapshot.Summary.PreferredSumPercentage.ShouldBe(33.3);
    }

    [Fact]
    public void AggregateDescribesTheLatestContest()
    {
        var snapshot = LotofacilDashboardAggregator.Aggregate(Draws);

        snapshot.LatestContest.ShouldNotBeNull();
        snapshot.LatestContest.ContestNumber.ShouldBe(3);
        snapshot.LatestContest.EvenCount.ShouldBe(12);
        snapshot.LatestContest.OddCount.ShouldBe(3);
        snapshot.LatestContest.Sum.ShouldBe(225);
        snapshot.LatestContest.PrimeCount.ShouldBe(2);
        snapshot.LatestContest.BorderCount.ShouldBe(11);
        snapshot.LatestContest.RepeatedFromPrevious.ShouldBe(6);
        snapshot.LatestContest.Numbers.Count.ShouldBe(15);
    }

    [Fact]
    public void AggregateReusesFilterCategories()
    {
        var snapshot = LotofacilDashboardAggregator.Aggregate(Draws);

        snapshot.Categories.ShouldContainKey("paridade");
        snapshot.Categories.ShouldContainKey("soma");
        snapshot.Categories["moldura"].Single(item => item.Value == 9).Count.ShouldBe(2);
    }

    [Fact]
    public void AggregateReturnsEmptySnapshotForNoDraws()
    {
        var snapshot = LotofacilDashboardAggregator.Aggregate([]);

        snapshot.TotalContests.ShouldBe(0);
        snapshot.LatestContest.ShouldBeNull();
        snapshot.Frequencies.ShouldBeEmpty();
        snapshot.Categories.ShouldBeEmpty();
    }
}
