using System.Net;
using System.Net.Http.Json;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace LotoAnalytics.Api.IntegrationTests;

public sealed class LotofacilDashboardEndpointTests
{
    [Fact]
    public async Task EndpointServesConsolidatedDashboardFromSavedContests()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateFactory(postgres.GetConnectionString());

        await SeedContestsAsync(factory);

        using var client = factory.CreateClient();
        var result = await client.GetFromJsonAsync<DashboardResponse>(
            "/estatisticas/lotofacil/painel",
            TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.CodigoModalidade.ShouldBe("lotofacil");
        result.TotalConcursos.ShouldBe(3);

        result.Frequencias.Count.ShouldBe(25);
        result.Frequencias.Single(item => item.Dezena == 2).Quantidade.ShouldBe(3);
        result.Frequencias.Single(item => item.Dezena == 2).Atraso.ShouldBe(0);
        result.Frequencias.Single(item => item.Dezena == 1).Atraso.ShouldBe(1);

        result.Resumo.SomaMedia.ShouldBe(179.0);
        result.Resumo.RepeticaoMedia.ShouldBe(7.5);
        result.Resumo.CombinacoesIneditasPercentual.ShouldBe(100.0);

        result.UltimoConcurso.ShouldNotBeNull();
        result.UltimoConcurso.Numero.ShouldBe(3);
        result.UltimoConcurso.Dezenas.Count.ShouldBe(15);

        result.Categorias.ShouldContainKey("paridade");
    }

    [Fact]
    public async Task EndpointReturnsEmptyDashboardWhenNoContestsExist()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/estatisticas/lotofacil/painel", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.TotalConcursos.ShouldBe(0);
        result.UltimoConcurso.ShouldBeNull();
        result.Frequencias.ShouldBeEmpty();
        result.Categorias.ShouldBeEmpty();
    }

    // Inicia um PostgreSQL isolado para validar migrations e consultas reais.
    private static async Task<PostgreSqlContainer> StartPostgresAsync()
    {
        var postgres = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("lotoanalytics")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await postgres.StartAsync(TestContext.Current.CancellationToken);

        return postgres;
    }

    // Cria a API apontando para o banco do teste, sem o atualizador em background.
    private static WebApplicationFactory<Program> CreateFactory(string connectionString)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
                builder.UseSetting("ContestUpdates:Enabled", "false");
            });
    }

    // Insere tres concursos com metricas conhecidas para exercitar o painel.
    private static async Task SeedContestsAsync(WebApplicationFactory<Program> factory)
    {
        int[][] draws =
        [
            [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15],
            [1, 2, 3, 5, 7, 9, 11, 13, 14, 17, 19, 20, 22, 24, 25],
            [2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 21, 22, 23, 24, 25]
        ];

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LotoAnalyticsDbContext>();
        var mode = await dbContext.LotteryModes.SingleAsync(
            candidate => candidate.Code == "lotofacil",
            TestContext.Current.CancellationToken);
        var now = DateTimeOffset.UtcNow;

        for (var index = 0; index < draws.Length; index++)
        {
            dbContext.Contests.Add(new Contest
            {
                LotteryModeId = mode.Id,
                Number = index + 1,
                DrawDate = new DateOnly(2026, 7, 20 + index),
                RawResultJson = "{}",
                CreatedAt = now,
                UpdatedAt = now,
                Numbers = draws[index]
                    .Select((value, position) => new ContestNumber
                    {
                        NumberType = "principal",
                        Position = position + 1,
                        Value = value.ToString("00"),
                        NumericValue = value,
                        CreatedAt = now
                    })
                    .ToList()
            });
        }

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed record DashboardResponse(
        string CodigoModalidade,
        int TotalConcursos,
        DashboardLatestContest? UltimoConcurso,
        DashboardSummary Resumo,
        List<DashboardFrequency> Frequencias,
        Dictionary<string, List<DashboardCategoryItem>> Categorias);

    private sealed record DashboardSummary(
        double SomaMedia,
        double RepeticaoMedia,
        double CombinacoesIneditasPercentual,
        double FaixaSomaPreferencialPercentual);

    private sealed record DashboardFrequency(int Dezena, int Quantidade, double Percentual, int Atraso, int? UltimoConcurso);

    private sealed record DashboardLatestContest(
        int Numero,
        DateOnly? DataApuracao,
        List<string> Dezenas,
        int Pares,
        int Impares,
        int Soma,
        int Primos,
        int Moldura,
        int RepetidasAnterior);

    private sealed record DashboardCategoryItem(int Valor, int Quantidade);
}
