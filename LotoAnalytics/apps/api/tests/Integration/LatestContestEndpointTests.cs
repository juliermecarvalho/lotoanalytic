using System.Net;
using System.Net.Http.Json;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace LotoAnalytics.Api.IntegrationTests;

public sealed class LatestContestEndpointTests
{
    [Fact]
    public async Task GetLatestContestReturnsMostRecentContestWithOrderedMainNumbers()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateFactory(postgres.GetConnectionString());

        await SeedContestsAsync(factory);

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/concursos/lotofacil/ultimo", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LatestContestResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.CodigoModalidade.ShouldBe("lotofacil");
        result.NumeroConcurso.ShouldBe(3411);
        result.DataApuracao.ShouldBe(new DateOnly(2026, 7, 24));
        result.TotalConcursos.ShouldBe(2);
        result.Dezenas.Count.ShouldBe(15);
        result.Dezenas[0].ShouldBe("01");
        result.Dezenas[^1].ShouldBe("25");
        result.Dezenas.ShouldBe(result.Dezenas.OrderBy(int.Parse).ToArray());
    }

    [Fact]
    public async Task GetLatestContestReturnsNotFoundWhenNoContestExists()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/concursos/lotofacil/ultimo", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // Inicia um PostgreSQL isolado para validar o endpoint com migrations reais.
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

    // Cria a API apontando para o banco do teste, sem o atualizador de concursos em background.
    private static WebApplicationFactory<Program> CreateFactory(string connectionString)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
                builder.UseSetting("ContestUpdates:Enabled", "false");
            });
    }

    // Insere dois concursos da Lotofacil, com dezenas fora de ordem para validar a ordenacao.
    private static async Task SeedContestsAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LotoAnalyticsDbContext>();
        var mode = await dbContext.LotteryModes.SingleAsync(
            candidate => candidate.Code == "lotofacil",
            TestContext.Current.CancellationToken);

        dbContext.Contests.Add(CreateContest(
            mode.Id,
            number: 3410,
            drawDate: new DateOnly(2026, 7, 22),
            mainNumbers: ["02", "04", "06", "08", "10", "12", "14", "16", "18", "20", "21", "22", "23", "24", "25"]));
        dbContext.Contests.Add(CreateContest(
            mode.Id,
            number: 3411,
            drawDate: new DateOnly(2026, 7, 24),
            mainNumbers: ["25", "01", "03", "05", "07", "09", "11", "13", "14", "17", "19", "20", "22", "24", "02"]));

        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    // Monta um concurso persistivel com as dezenas principais informadas.
    private static Contest CreateContest(Guid modeId, int number, DateOnly drawDate, string[] mainNumbers)
    {
        var now = DateTimeOffset.UtcNow;

        return new Contest
        {
            LotteryModeId = modeId,
            Number = number,
            DrawDate = drawDate,
            RawResultJson = "{}",
            CreatedAt = now,
            UpdatedAt = now,
            Numbers = mainNumbers
                .Select((value, index) => new ContestNumber
                {
                    NumberType = "principal",
                    Position = index + 1,
                    Value = value,
                    NumericValue = int.Parse(value),
                    CreatedAt = now
                })
                .ToList()
        };
    }

    private sealed record LatestContestResponse(
        string CodigoModalidade,
        int NumeroConcurso,
        DateOnly? DataApuracao,
        IReadOnlyList<string> Dezenas,
        int TotalConcursos);
}
