using System.Net;
using LotoAnalytics.Api.Features.Contests;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class CaixaLotteryClientTests
{
    [Fact]
    public async Task GetContestResultJsonCallsCaixaEndpointForLotteryModeAndContestNumber()
    {
        var handler = new StubHttpMessageHandler("""{"numero":3733}""");
        using var factory = StubHttpClientFactory.WithDirectRoute(handler);
        var client = CreateClient(factory);

        var json = await client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken);

        json.ShouldBe("""{"numero":3733}""");
        handler.RequestUris.Single().ShouldBe("https://servicebus3.caixa.gov.br/portaldeloterias/api/lotofacil/3733");
    }

    [Fact]
    public async Task GetContestResultJsonUsesConfiguredBaseUrl()
    {
        var handler = new StubHttpMessageHandler("""{"numero":3733}""");
        using var factory = StubHttpClientFactory.WithDirectRoute(handler);
        var client = CreateClient(factory, new CaixaLotteryOptions
        {
            BaseUrl = "https://relay.exemplo.com.br/portaldeloterias/api/"
        });

        await client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken);

        handler.RequestUris.Single().ShouldBe("https://relay.exemplo.com.br/portaldeloterias/api/lotofacil/3733");
    }

    [Fact]
    public async Task GetContestResultJsonMapsHttp500ToTransientApiException()
    {
        var handler = new StubHttpMessageHandler("""{"erro":true}""", HttpStatusCode.InternalServerError);
        using var factory = StubHttpClientFactory.WithDirectRoute(handler);
        var client = CreateClient(factory);

        var exception = await Should.ThrowAsync<CaixaTransientApiException>(
            () => client.GetContestResultJsonAsync("lotofacil", 3745, TestContext.Current.CancellationToken));

        exception.LotteryModeCode.ShouldBe("lotofacil");
        exception.ContestNumber.ShouldBe(3745);
    }

    [Fact]
    public async Task GetContestResultJsonMapsHttp403ToAccessBlockedExceptionInsteadOfTransient()
    {
        var handler = new StubHttpMessageHandler("<html>bloqueado</html>", HttpStatusCode.Forbidden);
        using var factory = StubHttpClientFactory.WithDirectRoute(handler);
        var client = CreateClient(factory);

        var exception = await Should.ThrowAsync<CaixaAccessBlockedException>(
            () => client.GetContestResultJsonAsync("lotofacil", 3745, TestContext.Current.CancellationToken));

        exception.RequestUri.ShouldBe("https://servicebus3.caixa.gov.br/portaldeloterias/api/lotofacil/3745");
        exception.ShouldNotBeAssignableTo<CaixaTransientApiException>();
    }

    [Fact]
    public async Task GetContestResultJsonFallsBackToNextProxyWhenFirstOneIsBlocked()
    {
        var blocked = new StubHttpMessageHandler("<html>bloqueado</html>", HttpStatusCode.Forbidden);
        var working = new StubHttpMessageHandler("""{"numero":3733}""");
        using var factory = new StubHttpClientFactory(new Dictionary<string, StubHttpMessageHandler>
        {
            ["caixa-proxy-0"] = blocked,
            ["caixa-proxy-1"] = working
        });
        var registry = new CaixaProxyRegistry(
            [
                new CaixaHttpRoute("caixa-proxy-0", "http://proxy-morto.exemplo:8080"),
                new CaixaHttpRoute("caixa-proxy-1", "http://proxy-bom.exemplo:8080")
            ],
            TimeSpan.FromMinutes(5));
        var client = CreateClient(factory, registry: registry);

        var json = await client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken);

        json.ShouldBe("""{"numero":3733}""");
        blocked.RequestUris.Count.ShouldBe(1);
        working.RequestUris.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetContestResultJsonSkipsProxyThatFailedRecently()
    {
        var broken = new StubHttpMessageHandler(string.Empty, HttpStatusCode.BadGateway);
        var working = new StubHttpMessageHandler("""{"numero":3733}""");
        using var factory = new StubHttpClientFactory(new Dictionary<string, StubHttpMessageHandler>
        {
            ["caixa-proxy-0"] = broken,
            ["caixa-proxy-1"] = working
        });
        var registry = new CaixaProxyRegistry(
            [
                new CaixaHttpRoute("caixa-proxy-0", "http://proxy-morto.exemplo:8080"),
                new CaixaHttpRoute("caixa-proxy-1", "http://proxy-bom.exemplo:8080")
            ],
            TimeSpan.FromMinutes(5));
        var client = CreateClient(factory, registry: registry);

        await client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken);
        await client.GetContestResultJsonAsync("lotofacil", 3734, TestContext.Current.CancellationToken);

        // O proxy quebrado so e tentado na primeira chamada; depois fica em quarentena.
        broken.RequestUris.Count.ShouldBe(1);
        working.RequestUris.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetContestResultJsonReportsBlockedAccessWhenEveryProxyIsBlocked()
    {
        var first = new StubHttpMessageHandler("<html>bloqueado</html>", HttpStatusCode.Forbidden);
        var second = new StubHttpMessageHandler("<html>bloqueado</html>", HttpStatusCode.Forbidden);
        using var factory = new StubHttpClientFactory(new Dictionary<string, StubHttpMessageHandler>
        {
            ["caixa-proxy-0"] = first,
            ["caixa-proxy-1"] = second
        });
        var registry = new CaixaProxyRegistry(
            [
                new CaixaHttpRoute("caixa-proxy-0", "http://proxy-a.exemplo:8080"),
                new CaixaHttpRoute("caixa-proxy-1", "http://proxy-b.exemplo:8080")
            ],
            TimeSpan.FromMinutes(5));
        var client = CreateClient(factory, registry: registry);

        await Should.ThrowAsync<CaixaAccessBlockedException>(
            () => client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken));

        first.RequestUris.Count.ShouldBe(1);
        second.RequestUris.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetContestResultJsonReportsTransientFailureWhenProxiesMixBlockAndInstability()
    {
        var blocked = new StubHttpMessageHandler("<html>bloqueado</html>", HttpStatusCode.Forbidden);
        var unstable = new StubHttpMessageHandler(string.Empty, HttpStatusCode.ServiceUnavailable);
        using var factory = new StubHttpClientFactory(new Dictionary<string, StubHttpMessageHandler>
        {
            ["caixa-proxy-0"] = blocked,
            ["caixa-proxy-1"] = unstable
        });
        var registry = new CaixaProxyRegistry(
            [
                new CaixaHttpRoute("caixa-proxy-0", "http://proxy-a.exemplo:8080"),
                new CaixaHttpRoute("caixa-proxy-1", "http://proxy-b.exemplo:8080")
            ],
            TimeSpan.FromMinutes(5));
        var client = CreateClient(factory, registry: registry);

        // Instabilidade parcial deve continuar sendo tratada como erro temporario, e nao como bloqueio.
        await Should.ThrowAsync<CaixaTransientApiException>(
            () => client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetContestResultJsonPropagatesNotFoundWithoutTryingOtherProxies()
    {
        var notFound = new StubHttpMessageHandler(string.Empty, HttpStatusCode.NotFound);
        var working = new StubHttpMessageHandler("""{"numero":3733}""");
        using var factory = new StubHttpClientFactory(new Dictionary<string, StubHttpMessageHandler>
        {
            ["caixa-proxy-0"] = notFound,
            ["caixa-proxy-1"] = working
        });
        var registry = new CaixaProxyRegistry(
            [
                new CaixaHttpRoute("caixa-proxy-0", "http://proxy-a.exemplo:8080"),
                new CaixaHttpRoute("caixa-proxy-1", "http://proxy-b.exemplo:8080")
            ],
            TimeSpan.FromMinutes(5));
        var client = CreateClient(factory, registry: registry);

        // Concurso inexistente e resposta legitima da Caixa: nao adianta perguntar a outro proxy.
        await Should.ThrowAsync<CaixaContestNotFoundException>(
            () => client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken));

        working.RequestUris.ShouldBeEmpty();
    }

    // Cria o client com as rotas e opcoes informadas ou com a conexao direta padrao.
    private static CaixaLotteryClient CreateClient(
        IHttpClientFactory httpClientFactory,
        CaixaLotteryOptions? options = null,
        CaixaProxyRegistry? registry = null)
    {
        return new CaixaLotteryClient(
            httpClientFactory,
            registry ?? new CaixaProxyRegistry(
                [new CaixaHttpRoute(CaixaHttpHandlerFactory.DirectClientName, null)],
                TimeSpan.FromMinutes(5)),
            Options.Create(options ?? new CaixaLotteryOptions()),
            TimeProvider.System,
            NullLogger<CaixaLotteryClient>.Instance);
    }

    private sealed class StubHttpClientFactory(IReadOnlyDictionary<string, StubHttpMessageHandler> handlers)
        : IHttpClientFactory, IDisposable
    {
        private readonly List<HttpClient> createdClients = [];

        // Cria uma fabrica com uma unica rota direta, equivalente ao ambiente sem proxy.
        public static StubHttpClientFactory WithDirectRoute(StubHttpMessageHandler handler)
        {
            return new StubHttpClientFactory(new Dictionary<string, StubHttpMessageHandler>
            {
                [CaixaHttpHandlerFactory.DirectClientName] = handler
            });
        }

        // Devolve um HttpClient ligado ao handler registrado para o nome da rota.
        public HttpClient CreateClient(string name)
        {
            var client = new HttpClient(handlers[name], disposeHandler: false);
            createdClients.Add(client);
            return client;
        }

        public void Dispose()
        {
            foreach (var client in createdClients)
            {
                client.Dispose();
            }
        }
    }

    private sealed class StubHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public List<string> RequestUris { get; } = [];

        // Retorna uma resposta HTTP controlada para testar o client sem rede.
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
