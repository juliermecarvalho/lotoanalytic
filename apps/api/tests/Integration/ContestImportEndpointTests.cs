using System.Net.Http.Json;
using LotoAnalytics.Api.Features.Contests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.IntegrationTests;

public sealed class ContestImportEndpointTests
{
    [Fact]
    public async Task ImportContestReturnsImportSummary()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IContestImportService>();
                    services.AddScoped<IContestImportService>(_ => new StubContestImportService());
                });
            });
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/concursos/lotofacil/3733/importar", null, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ContestImportEndpointResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.CodigoModalidade.ShouldBe("lotofacil");
        result.NumeroConcurso.ShouldBe(3733);
        result.QuantidadeDezenasPrincipal.ShouldBe(15);
        result.QuantidadeFaixasPremio.ShouldBe(5);
    }

    private sealed class StubContestImportService : IContestImportService
    {
        // Retorna uma importacao controlada para testar somente o contrato HTTP.
        public Task<ContestImportResult> ImportContestAsync(
            string modeCode,
            int contestNumber,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ContestImportResult(modeCode, contestNumber, 15, 5));
        }
    }

    private sealed record ContestImportEndpointResponse(
        string CodigoModalidade,
        int NumeroConcurso,
        int QuantidadeDezenasPrincipal,
        int QuantidadeFaixasPremio);
}
