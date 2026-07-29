using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Testcontainers.PostgreSql;
using Xunit;

namespace LotoAnalytics.Api.IntegrationTests;

public sealed class LotofacilGameCheckingEndpointTests
{
    [Fact]
    public async Task CheckLotofacilGamesReturnsHitsAndAwardSummary()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/conferencias/lotofacil/conferir",
            new
            {
                dezenasSorteadas = new[]
                {
                    "01", "02", "03", "04", "05",
                    "06", "07", "08", "09", "10",
                    "11", "12", "13", "14", "15"
                },
                jogos = new[]
                {
                    new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15" },
                    new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "16", "17", "18", "19" }
                }
            },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LotofacilGameCheckingResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Jogos.Select(game => game.QuantidadeAcertos).ShouldBe([15, 11]);
        result.Jogos[0].DezenasAcertadas.ShouldBe(["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15"]);
        result.ResumoPremiacao.ShouldContainKeyAndValue(15, 1);
        result.ResumoPremiacao.ShouldContainKeyAndValue(11, 1);
    }

    [Fact]
    public async Task CheckLotofacilGamesPersistsCheckingHistory()
    {
        await using var postgres = await StartPostgresAsync();
        await using var factory = CreateAuthenticatedFactory(postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/conferencias/lotofacil/conferir",
            new
            {
                dezenasSorteadas = new[]
                {
                    "01", "02", "03", "04", "05",
                    "06", "07", "08", "09", "10",
                    "11", "12", "13", "14", "15"
                },
                jogos = new[]
                {
                    new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15" },
                    new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "16", "17", "18", "19" }
                }
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var history = await client.GetFromJsonAsync<GameCheckingHistoryResponse>(
            "/usuarios/me/conferencias",
            TestContext.Current.CancellationToken);

        history.ShouldNotBeNull();
        history.Conferencias.Count.ShouldBe(1);
        history.Conferencias[0].QuantidadeJogos.ShouldBe(2);
        history.Conferencias[0].ResumoPremiacao.ShouldContainKeyAndValue(15, 1);
        history.Conferencias[0].ResumoPremiacao.ShouldContainKeyAndValue(11, 1);
        history.Conferencias[0].Jogos.Select(game => game.QuantidadeAcertos).ShouldBe([15, 11]);
    }

    // Inicia um PostgreSQL isolado para validar persistencia de conferencias.
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
    private static WebApplicationFactory<Program> CreateAuthenticatedFactory(string connectionString)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
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

    private sealed record LotofacilGameCheckingResponse(
        IReadOnlyList<CheckedGameResponse> Jogos,
        IReadOnlyDictionary<int, int> ResumoPremiacao);

    private sealed record CheckedGameResponse(
        int NumeroJogo,
        int QuantidadeAcertos,
        IReadOnlyList<string> DezenasAcertadas,
        bool Premiado);

    private sealed record GameCheckingHistoryResponse(IReadOnlyList<GameCheckingHistoryItemResponse> Conferencias);

    private sealed record GameCheckingHistoryItemResponse(
        Guid Id,
        int QuantidadeJogos,
        DateTimeOffset CriadoEm,
        IReadOnlyDictionary<int, int> ResumoPremiacao,
        IReadOnlyList<GameCheckingHistoryGameResponse> Jogos);

    private sealed record GameCheckingHistoryGameResponse(
        int NumeroJogo,
        int QuantidadeAcertos,
        IReadOnlyList<string> DezenasAcertadas);

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "Test";

        // Cria um principal autenticado previsivel para os testes do conferidor.
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "33333333-3333-3333-3333-333333333333"),
                new Claim("preferred_username", "conferidor.teste"),
                new Claim(ClaimTypes.Email, "conferidor.teste@lotoanalytics.local"),
                new Claim(ClaimTypes.Role, "usuario_gratis")
            };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
