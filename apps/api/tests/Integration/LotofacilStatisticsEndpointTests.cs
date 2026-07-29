using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.IntegrationTests;

public sealed class LotofacilStatisticsEndpointTests
{
    [Fact]
    public async Task CalculateLotofacilStatisticsReturnsComputedSummary()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/estatisticas/lotofacil/calcular",
            new
            {
                dezenas = new[]
                {
                    "01", "02", "03", "04", "05",
                    "06", "07", "08", "09", "10",
                    "11", "13", "17", "19", "23"
                },
                dezenasAnteriores = new[]
                {
                    "01", "02", "04", "06", "08",
                    "10", "12", "14", "16", "18",
                    "20", "21", "22", "23", "24"
                }
            },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LotofacilStatisticsResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.QuantidadePares.ShouldBe(5);
        result.QuantidadeImpares.ShouldBe(10);
        result.SomaDezenas.ShouldBe(138);
        result.RepetidasAnterior.ShouldBe(["01", "02", "04", "06", "08", "10", "23"]);
    }

    [Fact]
    public async Task CalculateLotofacilStatisticsRejectsTooFewNumbers()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/estatisticas/lotofacil/calcular",
            new { dezenas = new[] { "01", "02", "03" } },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    private sealed record LotofacilStatisticsResponse(
        int QuantidadePares,
        int QuantidadeImpares,
        int SomaDezenas,
        IReadOnlyList<string> RepetidasAnterior);
}
