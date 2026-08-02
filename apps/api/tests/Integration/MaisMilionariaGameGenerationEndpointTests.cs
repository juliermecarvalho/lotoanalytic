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

public sealed class MaisMilionariaGameGenerationEndpointTests
{
    [Fact]
    public async Task GenerateMaisMilionariaGamesReturnsGamesWithTrevosThatMatchFilters()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/gerador/mais-milionaria/gerar",
            new
            {
                quantidadeJogos = 2,
                dezenasPorJogo = 6,
                trevosPorJogo = 3,
                dezenasObrigatorias = new[] { "01", "02" },
                dezenasExcluidas = new[] { "50" },
                quantidadePares = 3,
                quantidadeImpares = 3,
                somaMinima = 60,
                somaMaxima = 200,
                primosMinimo = 1,
                primosMaximo = 4,
                sequenciaMaxima = 3
            },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<MaisMilionariaGameGenerationResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Jogos.Count.ShouldBe(2);
        result.CombinacoesTestadas.ShouldBeGreaterThan(0);
        result.Jogos.Select(game => string.Join(",", game.Dezenas)).Distinct().Count().ShouldBe(2);
        foreach (var game in result.Jogos)
        {
            game.Dezenas.Count.ShouldBe(6);
            game.Dezenas.ShouldContain("01");
            game.Dezenas.ShouldContain("02");
            game.Dezenas.ShouldNotContain("50");
            game.QuantidadePares.ShouldBe(3);
            game.QuantidadeImpares.ShouldBe(3);
            game.SomaDezenas.ShouldBeInRange(60, 200);
            game.QuantidadePrimos.ShouldBeInRange(1, 4);
            game.MaiorSequencia.ShouldBeLessThanOrEqualTo(3);

            game.Trevos.Count.ShouldBe(3);
            game.Trevos.Distinct().Count().ShouldBe(3);
            game.Trevos.ShouldAllBe(trevo => int.Parse(trevo) >= 1 && int.Parse(trevo) <= 6);
        }
    }

    [Fact]
    public async Task GenerateMaisMilionariaGamesRejectsTrevoCountBelowTheMinimum()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/gerador/mais-milionaria/gerar",
            new
            {
                quantidadeJogos = 1,
                dezenasPorJogo = 6,
                trevosPorJogo = 1
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateMaisMilionariaGamesPersistsGenerationHistory()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var generationResponse = await client.PostAsJsonAsync(
            "/gerador/mais-milionaria/gerar",
            new
            {
                quantidadeJogos = 2,
                dezenasPorJogo = 6,
                trevosPorJogo = 2,
                dezenasObrigatorias = new[] { "01", "02" }
            },
            TestContext.Current.CancellationToken);
        generationResponse.EnsureSuccessStatusCode();

        var history = await client.GetFromJsonAsync<GameGenerationHistoryResponse>(
            "/usuarios/me/geracoes",
            TestContext.Current.CancellationToken);

        history.ShouldNotBeNull();
        history.Geracoes.Count.ShouldBe(1);
        history.Geracoes[0].QuantidadeJogos.ShouldBe(2);
        history.Geracoes[0].DezenasPorJogo.ShouldBe(6);
        history.Geracoes[0].Jogos.Count.ShouldBe(2);
        history.Geracoes[0].Jogos.Select(game => string.Join(",", game.Dezenas)).Distinct().Count().ShouldBe(2);
    }

    // Inicia um PostgreSQL isolado para validar migrations e o fluxo de geracao da +Milionaria.
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

    private sealed record MaisMilionariaGameGenerationResponse(
        IReadOnlyList<GeneratedGameResponse> Jogos,
        int CombinacoesTestadas);

    private sealed record GeneratedGameResponse(
        IReadOnlyList<string> Dezenas,
        IReadOnlyList<string> Trevos,
        int QuantidadePares,
        int QuantidadeImpares,
        int SomaDezenas,
        int QuantidadeRepetidas,
        int QuantidadePrimos,
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
                new Claim(ClaimTypes.NameIdentifier, "55555555-5555-5555-5555-555555555555"),
                new Claim("preferred_username", "gerador.mais-milionaria"),
                new Claim(ClaimTypes.Email, "gerador.mais-milionaria@lotoanalytics.local"),
                new Claim(ClaimTypes.Role, Context.RequestServices.GetRequiredService<IConfiguration>()["TestAuth:Role"] ?? "usuario_gratis")
            };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
