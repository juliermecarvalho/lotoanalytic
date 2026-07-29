using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace LotoAnalytics.Api.IntegrationTests;

public sealed class LotofacilGameGenerationEndpointTests
{
    [Fact]
    public async Task GenerateLotofacilGamesReturnsGamesThatMatchFilters()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/gerador/lotofacil/gerar",
            new
            {
                quantidadeJogos = 2,
                dezenasPorJogo = 15,
                dezenasObrigatorias = new[] { "01", "02" },
                dezenasExcluidas = new[] { "25" },
                dezenasAnteriores = new[] { "01", "02", "03", "05", "07", "09", "11", "13", "14", "17", "19", "20", "22", "24" },
                quantidadePares = 7,
                quantidadeImpares = 8,
                somaMinima = 120,
                somaMaxima = 210,
                repetidasMinima = 8,
                repetidasMaxima = 10,
                primosMinimo = 4,
                primosMaximo = 7,
                molduraMinima = 8,
                molduraMaxima = 11,
                sequenciaMaxima = 5,
                faixasSoma = new[] { new { somaMinima = 170, somaMaxima = 220 } },
                apenasIneditos = true
            },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LotofacilGameGenerationResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Jogos.Count.ShouldBe(2);
        result.CombinacoesTestadas.ShouldBeGreaterThan(0);
        result.Jogos.Select(game => string.Join(",", game.Dezenas)).Distinct().Count().ShouldBe(2);
        foreach (var game in result.Jogos)
        {
            game.Dezenas.Count.ShouldBe(15);
            game.Dezenas.ShouldContain("01");
            game.Dezenas.ShouldContain("02");
            game.Dezenas.ShouldNotContain("25");
            game.QuantidadePares.ShouldBe(7);
            game.QuantidadeImpares.ShouldBe(8);
            game.SomaDezenas.ShouldBeInRange(170, 220);
            game.QuantidadeRepetidas.ShouldBeInRange(8, 10);
            game.QuantidadePrimos.ShouldBeInRange(4, 7);
            game.QuantidadeMoldura.ShouldBeInRange(8, 11);
            game.MaiorSequencia.ShouldBeLessThanOrEqualTo(5);
        }
    }

    [Fact]
    public async Task GenerateLotofacilGamesPersistsGenerationHistory()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var generationResponse = await client.PostAsJsonAsync(
            "/gerador/lotofacil/gerar",
            new
            {
                quantidadeJogos = 2,
                dezenasPorJogo = 15,
                dezenasObrigatorias = new[] { "01", "02" },
                dezenasExcluidas = new[] { "25" }
            },
            TestContext.Current.CancellationToken);
        generationResponse.EnsureSuccessStatusCode();

        var history = await client.GetFromJsonAsync<GameGenerationHistoryResponse>(
            "/usuarios/me/geracoes",
            TestContext.Current.CancellationToken);

        history.ShouldNotBeNull();
        history.Geracoes.Count.ShouldBe(1);
        history.Geracoes[0].QuantidadeJogos.ShouldBe(2);
        history.Geracoes[0].DezenasPorJogo.ShouldBe(15);
        history.Geracoes[0].Jogos.Count.ShouldBe(2);
        history.Geracoes[0].Jogos.Select(game => string.Join(",", game.Dezenas)).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task ExportLotofacilGenerationCsvRejectsFreePlan()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var generationId = await GenerateAndReadFirstGenerationIdAsync(client);

        var response = await client.GetAsync(
            $"/usuarios/me/geracoes/{generationId}/exportar-csv",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExportLotofacilGenerationCsvReturnsCsvForPremiumPlan()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString(), "usuario_premium");
        using var client = factory.CreateClient();

        var generationId = await GenerateAndReadFirstGenerationIdAsync(client);

        var response = await client.GetAsync(
            $"/usuarios/me/geracoes/{generationId}/exportar-csv",
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/csv");
        var csv = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        csv.ShouldStartWith("numero_jogo,dezenas,soma_dezenas");
        csv.ShouldContain("1,");
        csv.ShouldContain("01");
    }

    [Fact]
    public async Task GenerateLotofacilGamesAllowsPublicRequestsUpToScreenLimit()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/gerador/lotofacil/gerar",
            new
            {
                quantidadeJogos = 6,
                dezenasPorJogo = 15
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Inicia um PostgreSQL isolado para validar migrations e regras por plano.
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

    // Cria uma API autenticada com um usuario de teste sincronizado no banco.
    private static WebApplicationFactory<Program> CreateAuthenticatedFactory(string connectionString, string role = "usuario_gratis")
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("TestAuth:Role", role);
                builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
                builder.UseSetting("ContestUpdates:Enabled", "false");
                builder.ConfigureTestServices(services =>
                {
                    services
                        .AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.AuthenticationScheme,
                            configureOptions: null);
                });
            });
    }

    // Gera dois jogos e devolve o identificador da primeira geracao persistida.
    private static async Task<Guid> GenerateAndReadFirstGenerationIdAsync(HttpClient client)
    {
        var generationResponse = await client.PostAsJsonAsync(
            "/gerador/lotofacil/gerar",
            new
            {
                quantidadeJogos = 2,
                dezenasPorJogo = 15,
                dezenasObrigatorias = new[] { "01", "02" },
                dezenasExcluidas = new[] { "25" }
            },
            TestContext.Current.CancellationToken);
        generationResponse.EnsureSuccessStatusCode();

        var history = await client.GetFromJsonAsync<GameGenerationHistoryResponse>(
            "/usuarios/me/geracoes",
            TestContext.Current.CancellationToken);

        history.ShouldNotBeNull();
        return history.Geracoes.Single().Id;
    }

    private sealed record LotofacilGameGenerationResponse(
        IReadOnlyList<GeneratedGameResponse> Jogos,
        int CombinacoesTestadas);

    private sealed record GeneratedGameResponse(
        IReadOnlyList<string> Dezenas,
        int QuantidadePares,
        int QuantidadeImpares,
        int SomaDezenas,
        int QuantidadeRepetidas,
        int QuantidadePrimos,
        int QuantidadeMoldura,
        int MaiorSequencia);

    private sealed record GameGenerationHistoryResponse(IReadOnlyList<GameGenerationHistoryItemResponse> Geracoes);

    private sealed record GameGenerationHistoryItemResponse(
        Guid Id,
        int QuantidadeJogos,
        int DezenasPorJogo,
        DateTimeOffset CriadoEm,
        IReadOnlyList<GameGenerationHistoryGameResponse> Jogos);

    private sealed record GameGenerationHistoryGameResponse(
        int NumeroJogo,
        IReadOnlyList<string> Dezenas,
        int SomaDezenas);

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "Test";

        // Cria um principal autenticado previsivel para os testes do gerador.
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "22222222-2222-2222-2222-222222222222"),
                new Claim("preferred_username", "gerador.teste"),
                new Claim(ClaimTypes.Email, "gerador.teste@lotoanalytics.local"),
                new Claim(ClaimTypes.Role, Context.RequestServices.GetRequiredService<IConfiguration>()["TestAuth:Role"] ?? "usuario_gratis")
            };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
