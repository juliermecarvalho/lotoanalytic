using LotoAnalytics.Api.Features.FilterStatistics;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class FilterStatisticsAggregatorTests
{
    // Tres sorteios com metricas conhecidas calculadas manualmente.
    private static readonly int[][] Draws =
    [
        [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15],
        [1, 2, 3, 5, 7, 9, 11, 13, 14, 17, 19, 20, 22, 24, 25],
        [2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 21, 22, 23, 24, 25]
    ];

    [Fact]
    public void AggregateComputesAllEightSummariesFromOrderedDraws()
    {
        var buckets = FilterStatisticsAggregator.Aggregate(Draws);

        CountOf(buckets, "paridade", 7).ShouldBe(1);
        CountOf(buckets, "paridade", 5).ShouldBe(1);
        CountOf(buckets, "paridade", 12).ShouldBe(1);
        SumOf(buckets, "paridade").ShouldBe(3);

        // Repeticao usa pares consecutivos: sorteio 2 repete 9 do sorteio 1; sorteio 3 repete 6 do sorteio 2.
        CountOf(buckets, "repeticao", 9).ShouldBe(1);
        CountOf(buckets, "repeticao", 6).ShouldBe(1);
        SumOf(buckets, "repeticao").ShouldBe(2);

        CountOf(buckets, "primos", 6).ShouldBe(1);
        CountOf(buckets, "primos", 8).ShouldBe(1);
        CountOf(buckets, "primos", 2).ShouldBe(1);

        CountOf(buckets, "moldura", 9).ShouldBe(2);
        CountOf(buckets, "moldura", 11).ShouldBe(1);

        CountOf(buckets, "soma", 120).ShouldBe(1);
        CountOf(buckets, "soma", 192).ShouldBe(1);
        CountOf(buckets, "soma", 225).ShouldBe(1);

        // Grade: sorteio 1 tem linha zerada (classe 3), sorteio 2 e equilibrado (classe 0),
        // sorteio 3 tem linha com 5 dezenas (classe 2).
        CountOf(buckets, "grade", 3).ShouldBe(1);
        CountOf(buckets, "grade", 0).ShouldBe(1);
        CountOf(buckets, "grade", 2).ShouldBe(1);

        CountOf(buckets, "sequencia", 15).ShouldBe(1);
        CountOf(buckets, "sequencia", 3).ShouldBe(1);
        CountOf(buckets, "sequencia", 6).ShouldBe(1);
    }

    [Fact]
    public void AggregateReturnsNoBucketsForAnEmptyBase()
    {
        FilterStatisticsAggregator.Aggregate([]).ShouldBeEmpty();
    }

    [Fact]
    public void AggregateSkipsRepetitionWhenThereIsOnlyOneDraw()
    {
        var buckets = FilterStatisticsAggregator.Aggregate([Draws[0]]);

        SumOf(buckets, "repeticao").ShouldBe(0);
        SumOf(buckets, "paridade").ShouldBe(1);
    }

    // Soma as quantidades de uma categoria para validar o total de amostras.
    private static int SumOf(IReadOnlyList<FilterStatisticBucket> buckets, string category)
    {
        return buckets.Where(bucket => bucket.Category == category).Sum(bucket => bucket.Count);
    }

    // Busca a quantidade registrada para um valor especifico da categoria.
    private static int CountOf(IReadOnlyList<FilterStatisticBucket> buckets, string category, int value)
    {
        return buckets.SingleOrDefault(bucket => bucket.Category == category && bucket.Value == value)?.Count ?? 0;
    }
}
