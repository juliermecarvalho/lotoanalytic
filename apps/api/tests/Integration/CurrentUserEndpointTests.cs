using System.Net;
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

public sealed class CurrentUserEndpointTests
{
    [Fact]
    public async Task GetCurrentUserRequiresAuthentication()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/usuarios/me", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUserSynchronizesAuthenticatedUser()
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
                builder.ConfigureTestServices(services =>
                {
                    services
                        .AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.AuthenticationScheme,
                            configureOptions: null);
                });
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/usuarios/me", TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.Subject.ShouldBe("11111111-1111-1111-1111-111111111111");
        result.Username.ShouldBe("usuario.teste");
        result.Email.ShouldBe("usuario.teste@lotoanalytics.local");
        result.Roles.ShouldBe(["usuario_premium"]);
        result.Id.ShouldNotBe(Guid.Empty);
        result.UltimoLoginEm.ShouldNotBeNull();
        result.PlanoAtual.ShouldNotBeNull();
        result.PlanoAtual.Codigo.ShouldBe("premium");
        result.PlanoAtual.Nome.ShouldBe("Premium");
        result.PlanoAtual.LimiteJogosPorGeracao.ShouldBe(100);
        result.PlanoAtual.PermiteExportarCsv.ShouldBeTrue();
        result.PlanoAtual.PermiteExportarPdf.ShouldBeTrue();

        var secondResponse = await client.GetFromJsonAsync<CurrentUserResponse>("/usuarios/me", TestContext.Current.CancellationToken);

        secondResponse.ShouldNotBeNull();
        secondResponse.Id.ShouldBe(result.Id);
    }

    private sealed record CurrentUserResponse(
        Guid Id,
        string Subject,
        string? Username,
        string? Email,
        IReadOnlyList<string> Roles,
        DateTimeOffset? UltimoLoginEm,
        CurrentPlanResponse? PlanoAtual);

    private sealed record CurrentPlanResponse(
        string Codigo,
        string Nome,
        int LimiteJogosPorGeracao,
        bool PermiteExportarCsv,
        bool PermiteExportarPdf);

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "Test";

        // Cria um principal autenticado previsivel para os testes de integracao.
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "11111111-1111-1111-1111-111111111111"),
                new Claim("preferred_username", "usuario.teste"),
                new Claim(ClaimTypes.Email, "usuario.teste@lotoanalytics.local"),
                new Claim(ClaimTypes.Role, "usuario_premium")
            };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
