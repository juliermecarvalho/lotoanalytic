using System.Net.Http.Json;
using LotoAnalytics.Api.Features.FilterStatistics;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace LotoAnalytics.Api.IntegrationTests;

public sealed class FilterStatisticsEndpointTests
{
    [Fact]
    public async Task RefreshPersistsDistributionsAndEndpointServesThem()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateFactory(postgres.GetConnectionString());

        await SeedContestsAsync(factory);
        await RefreshStatisticsAsync(factory);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/estatisticas/lotofacil/filtros", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FilterStatisticsResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.CodigoModalidade.ShouldBe("lotofacil");
        result.TotalConcursos.ShouldBe(3);
        result.AtualizadoEm.ShouldNotBeNull();

        // Sorteios do seed: pares 7, 5 e 12 — um concurso em cada balde.
        result.Categorias.ShouldContainKey("paridade");
        result.Categorias["paridade"].Single(item => item.Valor == 7).Quantidade.ShouldBe(1);
        result.Categorias["paridade"].Single(item => item.Valor == 5).Quantidade.ShouldBe(1);
        result.Categorias["paridade"].Single(item => item.Valor == 12).Quantidade.ShouldBe(1);

        // Repeticao usa apenas os pares consecutivos: 9 e depois 6 dezenas repetidas.
        result.Categorias["repeticao"].Sum(item => item.Quantidade).ShouldBe(2);
        result.Categorias["repeticao"].Single(item => item.Valor == 9).Quantidade.ShouldBe(1);

        result.Categorias["primos"].Sum(item => item.Quantidade).ShouldBe(3);
        result.Categorias["moldura"].Single(item => item.Valor == 9).Quantidade.ShouldBe(2);
        result.Categorias["soma"].Single(item => item.Valor == 192).Quantidade.ShouldBe(1);
        result.Categorias["grade"].Sum(item => item.Quantidade).ShouldBe(3);
        result.Categorias["sequencia"].Single(item => item.Valor == 15).Quantidade.ShouldBe(1);
    }

    [Fact]
    public async Task EndpointReturnsEmptyDistributionsWhenTableWasNeverRefreshed()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/estatisticas/lotofacil/filtros", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FilterStatisticsResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.TotalConcursos.ShouldBe(0);
        result.Categorias.ShouldBeEmpty();
        result.AtualizadoEm.ShouldBeNull();
    }

    [Fact]
    public async Task ApiStartupBackfillsStatisticsForAPreExistingContestBase()
    {
        await using var postgres = await StartPostgresAsync();

        // Primeira subida: apenas popula concursos, sem nunca rodar o refresh (base pre-existente).
        await using (var seedFactory = CreateFactory(postgres.GetConnectionString()))
        {
            await SeedContestsAsync(seedFactory);
        }

        // Segunda subida: o backfill de inicializacao deve calcular as estatisticas sozinho.
        await using var factory = CreateFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<FilterStatisticsResponse>(
            "/estatisticas/lotofacil/filtros",
            TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.TotalConcursos.ShouldBe(3);
        result.Categorias.ShouldContainKey("paridade");
    }

    [Fact]
    public async Task RefreshReplacesPreviousDistributionsInsteadOfAccumulating()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateFactory(postgres.GetConnectionString());

        await SeedContestsAsync(factory);
        await RefreshStatisticsAsync(factory);
        await RefreshStatisticsAsync(factory);

        using var client = factory.CreateClient();
        var result = await client.GetFromJsonAsync<FilterStatisticsResponse>(
            "/estatisticas/lotofacil/filtros",
            TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.TotalConcursos.ShouldBe(3);
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

    // Executa o servico de refresh como o atualizador de concursos faria apos importar.
    private static async Task RefreshStatisticsAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var refreshService = scope.ServiceProvider.GetRequiredService<IFilterStatisticsRefreshService>();
        await refreshService.RefreshAsync("lotofacil", TestContext.Current.CancellationToken);
    }

    // Insere tres concursos com metricas conhecidas (mesmo conjunto dos testes unitarios).
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

    private sealed record FilterStatisticsResponse(
        string CodigoModalidade,
        int TotalConcursos,
        DateTimeOffset? AtualizadoEm,
        Dictionary<string, List<FilterStatisticsItem>> Categorias);

    private sealed record FilterStatisticsItem(int Valor, int Quantidade);
}
