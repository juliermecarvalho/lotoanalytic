using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace LotoAnalytics.Api.IntegrationTests;

public sealed class ModalidadesEndpointTests
{
    [Fact]
    public async Task GetModalidadesReturnsSeededModalidades()
    {
        await using var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("lotoanalytics")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await postgres.StartAsync(TestContext.Current.CancellationToken);

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", postgres.GetConnectionString());
                builder.UseSetting("ContestUpdates:Enabled", "false");
            });

        using var client = factory.CreateClient();

        var modalidades = await client.GetFromJsonAsync<List<ModalidadeResponse>>("/modalidades", TestContext.Current.CancellationToken);

        modalidades.ShouldNotBeNull();
        modalidades.Select(modalidade => modalidade.Codigo).ShouldBe(
        [
            "lotofacil",
            "mega_sena",
            "quina",
            "maismilionaria",
            "lotomania",
            "timemania",
            "dupla_sena",
            "dia_de_sorte",
            "super_sete"
        ]);
        var lotofacil = modalidades.Single(modalidade => modalidade.Codigo == "lotofacil");
        lotofacil.QuantidadeDezenasPrincipal.ShouldBe(15);
        lotofacil.ValorApostaSimples.ShouldBe(3.50m);
    }

    private sealed record ModalidadeResponse(
        string Codigo,
        string Nome,
        string TipoJogoCaixa,
        int QuantidadeDezenasPrincipal,
        decimal? ValorApostaSimples,
        bool Ativa);
}
