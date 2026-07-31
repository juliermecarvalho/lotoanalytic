using System.Net;

namespace LotoAnalytics.Api.Features.Contests;

public static class CaixaHttpHandlerFactory
{
    // Monta o handler HTTP das chamadas da Caixa, ativando o proxy apenas quando ele estiver configurado.
    public static HttpMessageHandler Create(CaixaProxyOptions proxyOptions)
    {
        if (!proxyOptions.Enabled || string.IsNullOrWhiteSpace(proxyOptions.Address))
        {
            return new HttpClientHandler();
        }

        var proxy = new WebProxy(proxyOptions.Address);

        if (!string.IsNullOrWhiteSpace(proxyOptions.Username))
        {
            proxy.Credentials = new NetworkCredential(proxyOptions.Username, proxyOptions.Password ?? string.Empty);
        }

        return new HttpClientHandler
        {
            Proxy = proxy,
            UseProxy = true
        };
    }
}
