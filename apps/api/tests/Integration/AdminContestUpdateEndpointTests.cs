using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using LotoAnalytics.Api.Features.Contests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.IntegrationTests;

public sealed class AdminContestUpdateEndpointTests
{
    [Fact]
    public async Task UpdateAllRequiresAdministratorRole()
    {
        await using var factory = CreateFactory("usuario_premium");
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/admin/concursos/atualizar-todos",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateAllReturnsBulkUpdateSummaryForAdministrator()
    {
        await using var factory = CreateFactory("administrador");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/admin/concursos/atualizar-todos",
            new { limitePorModalidade = 1, pausaMs = 0, pausaErroMs = 0, maxTentativasErro = 1 },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ContestBulkUpdateEndpointResponse>(TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.TotalImportado.ShouldBe(1);
        result.Modalidades.Single().CodigoModalidade.ShouldBe("lotofacil");
        result.Modalidades.Single().ConcursosImportados.ShouldBe([3734]);
    }

    [Fact]
    public async Task UpdateAllWithProgressStreamsOneEventPerImportedContest()
    {
        await using var factory = CreateFactory("administrador");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/admin/concursos/atualizar-todos/progresso",
            new { limitePorModalidade = 1, pausaMs = 0, pausaErroMs = 0, maxTentativasErro = 1 },
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/x-ndjson");

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Length.ShouldBe(4);

        var started = JsonDocument.Parse(lines[0]).RootElement;
        started.GetProperty("evento").GetString().ShouldBe("modalidade_iniciada");
        started.GetProperty("indiceModalidade").GetInt32().ShouldBe(1);
        started.GetProperty("totalModalidades").GetInt32().ShouldBe(1);
        started.GetProperty("retomarDoConcurso").GetInt32().ShouldBe(3734);
        started.GetProperty("ultimoConcursoSalvo").GetInt32().ShouldBe(3733);

        var imported = JsonDocument.Parse(lines[1]).RootElement;
        imported.GetProperty("evento").GetString().ShouldBe("concurso_importado");
        imported.GetProperty("codigoModalidade").GetString().ShouldBe("lotofacil");
        imported.GetProperty("numeroConcurso").GetInt32().ShouldBe(3734);
        imported.GetProperty("quantidadeImportada").GetInt32().ShouldBe(1);
        imported.GetProperty("dezenas").GetArrayLength().ShouldBe(3);

        var modeCompleted = JsonDocument.Parse(lines[2]).RootElement;
        modeCompleted.GetProperty("evento").GetString().ShouldBe("modalidade_concluida");
        modeCompleted.GetProperty("status").GetString().ShouldBe("atualizado");
        modeCompleted.GetProperty("proximoConcurso").GetInt32().ShouldBe(3735);
        modeCompleted.GetProperty("totalNoBanco").GetInt32().ShouldBe(3734);

        var completed = JsonDocument.Parse(lines[3]).RootElement;
        completed.GetProperty("evento").GetString().ShouldBe("concluido");
        completed.GetProperty("resultado").GetProperty("totalImportado").GetInt32().ShouldBe(1);
        completed.GetProperty("resultado").GetProperty("modalidades").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task UpdateAllWithProgressRequiresAdministratorRole()
    {
        await using var factory = CreateFactory("usuario_premium");
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/admin/concursos/atualizar-todos/progresso",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // Cria uma API autenticada com servico de atualizacao controlado para testar o contrato HTTP.
    private static WebApplicationFactory<Program> CreateFactory(string role)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("TestAuth:Role", role);
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IContestBulkUpdateService>();
                    services.AddScoped<IContestBulkUpdateService, StubContestBulkUpdateService>();
                    services
                        .AddAuthentication(TestAuthenticationHandler.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.AuthenticationScheme,
                            configureOptions: null);
                });
            });
    }

    private sealed record ContestBulkUpdateEndpointResponse(
        int TotalImportado,
        IReadOnlyList<ContestBulkUpdateModeEndpointResponse> Modalidades);

    private sealed record ContestBulkUpdateModeEndpointResponse(
        string CodigoModalidade,
        IReadOnlyList<int> ConcursosImportados);

    private sealed class StubContestBulkUpdateService : IContestBulkUpdateService
    {
        // Retorna uma atualizacao controlada, emitindo o progresso de cada concurso importado.
        public async Task<ContestBulkUpdateResult> UpdateAllAsync(
            ContestBulkUpdateRequest request,
            CancellationToken cancellationToken,
            ContestBulkUpdateProgressCallback? onProgress = null)
        {
            if (onProgress is not null)
            {
                await onProgress(new ContestBulkUpdateProgress(
                    Event: "modalidade_iniciada",
                    ModeCode: "lotofacil",
                    ModeName: "Lotofacil",
                    ModeIndex: 1,
                    ModeCount: 1,
                    ContestNumber: null,
                    MainNumbers: null,
                    ImportedInMode: 0,
                    ResumeFromContestNumber: 3734,
                    LastSavedContestNumber: 3733,
                    NextContestNumber: null,
                    TotalInDatabase: null,
                    Status: null,
                    Error: null));
                await onProgress(new ContestBulkUpdateProgress(
                    Event: "concurso_importado",
                    ModeCode: "lotofacil",
                    ModeName: "Lotofacil",
                    ModeIndex: 1,
                    ModeCount: 1,
                    ContestNumber: 3734,
                    MainNumbers: ["01", "02", "03"],
                    ImportedInMode: 1,
                    ResumeFromContestNumber: null,
                    LastSavedContestNumber: null,
                    NextContestNumber: null,
                    TotalInDatabase: null,
                    Status: null,
                    Error: null));
                await onProgress(new ContestBulkUpdateProgress(
                    Event: "modalidade_concluida",
                    ModeCode: "lotofacil",
                    ModeName: "Lotofacil",
                    ModeIndex: 1,
                    ModeCount: 1,
                    ContestNumber: null,
                    MainNumbers: null,
                    ImportedInMode: 1,
                    ResumeFromContestNumber: null,
                    LastSavedContestNumber: null,
                    NextContestNumber: 3735,
                    TotalInDatabase: 3734,
                    Status: "atualizado",
                    Error: null));
            }

            return new ContestBulkUpdateResult(
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                [
                    new ContestBulkUpdateModeResult(
                        "lotofacil",
                        "Lotofacil",
                        3734,
                        3735,
                        [3734],
                        "atualizado",
                        null)
                ]);
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "Test";

        // Cria um principal autenticado com papel configurado no teste.
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var role = Context.RequestServices.GetRequiredService<IConfiguration>()["TestAuth:Role"] ?? "usuario_premium";
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "33333333-3333-3333-3333-333333333333"),
                new Claim("preferred_username", "admin.teste"),
                new Claim(ClaimTypes.Email, "admin.teste@lotoanalytics.local"),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
