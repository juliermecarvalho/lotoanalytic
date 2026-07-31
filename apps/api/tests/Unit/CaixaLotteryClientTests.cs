using System.Net;
using LotoAnalytics.Api.Features.Contests;
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
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var json = await client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken);

        json.ShouldBe("""{"numero":3733}""");
        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.ToString().ShouldBe("https://servicebus3.caixa.gov.br/portaldeloterias/api/lotofacil/3733");
    }

    [Fact]
    public async Task GetContestResultJsonUsesConfiguredBaseUrl()
    {
        var handler = new StubHttpMessageHandler("""{"numero":3733}""");
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient, new CaixaLotteryOptions
        {
            BaseUrl = "https://relay.exemplo.com.br/portaldeloterias/api"
        });

        await client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken);

        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.ToString().ShouldBe("https://relay.exemplo.com.br/portaldeloterias/api/lotofacil/3733");
    }

    [Fact]
    public async Task GetContestResultJsonIgnoresTrailingSlashInConfiguredBaseUrl()
    {
        var handler = new StubHttpMessageHandler("""{"numero":3733}""");
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient, new CaixaLotteryOptions
        {
            BaseUrl = "https://relay.exemplo.com.br/api/"
        });

        await client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken);

        handler.RequestUri.ShouldNotBeNull();
        handler.RequestUri.ToString().ShouldBe("https://relay.exemplo.com.br/api/lotofacil/3733");
    }

    [Fact]
    public async Task GetContestResultJsonMapsHttp500ToTransientApiException()
    {
        var handler = new StubHttpMessageHandler("""{"erro":true}""", HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Should.ThrowAsync<CaixaTransientApiException>(
            () => client.GetContestResultJsonAsync("lotofacil", 3745, TestContext.Current.CancellationToken));

        exception.LotteryModeCode.ShouldBe("lotofacil");
        exception.ContestNumber.ShouldBe(3745);
    }

    [Fact]
    public async Task GetContestResultJsonMapsHttp403ToAccessBlockedExceptionInsteadOfTransient()
    {
        var handler = new StubHttpMessageHandler("<html>bloqueado</html>", HttpStatusCode.Forbidden);
        using var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var exception = await Should.ThrowAsync<CaixaAccessBlockedException>(
            () => client.GetContestResultJsonAsync("lotofacil", 3745, TestContext.Current.CancellationToken));

        exception.LotteryModeCode.ShouldBe("lotofacil");
        exception.ContestNumber.ShouldBe(3745);
        exception.RequestUri.ShouldBe("https://servicebus3.caixa.gov.br/portaldeloterias/api/lotofacil/3745");
        exception.ShouldNotBeAssignableTo<CaixaTransientApiException>();
    }

    // Cria o client com as opcoes informadas ou com o endereco padrao da Caixa.
    private static CaixaLotteryClient CreateClient(HttpClient httpClient, CaixaLotteryOptions? options = null)
    {
        return new CaixaLotteryClient(httpClient, Options.Create(options ?? new CaixaLotteryOptions()));
    }

    private sealed class StubHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        // Retorna uma resposta HTTP controlada para testar o client sem rede.
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
