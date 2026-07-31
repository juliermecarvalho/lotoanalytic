using System.Net;

namespace LotoAnalytics.Api.Features.Contests;

public static class CaixaHttpHandlerFactory
{
    public const string DirectClientName = "caixa-direct";

    // Monta o handler HTTP de uma rota, ativando o proxy apenas quando um endereco for informado.
    public static HttpMessageHandler Create(string? proxyAddress, CaixaProxyOptions proxyOptions)
    {
        if (string.IsNullOrWhiteSpace(proxyAddress))
        {
            return new HttpClientHandler();
        }

        var proxy = new WebProxy(proxyAddress);

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

    // Descreve as rotas de saida configuradas: uma por proxy ou uma unica conexao direta.
    public static IReadOnlyList<CaixaHttpRoute> BuildRoutes(CaixaProxyOptions proxyOptions)
    {
        if (!proxyOptions.Enabled)
        {
            return [new CaixaHttpRoute(DirectClientName, null)];
        }

        var addresses = proxyOptions.ResolveAddresses();
        if (addresses.Count == 0)
        {
            return [new CaixaHttpRoute(DirectClientName, null)];
        }

        return [.. addresses.Select((address, index) => new CaixaHttpRoute($"caixa-proxy-{index}", address))];
    }
}

public static class CaixaLotteryServiceCollectionExtensions
{
    // Registra o client da Caixa com uma rota HTTP por proxy configurado.
    public static IServiceCollection AddCaixaLotteryClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(CaixaLotteryOptions.SectionName);
        services.Configure<CaixaLotteryOptions>(section);

        var caixaOptions = section.Get<CaixaLotteryOptions>() ?? new CaixaLotteryOptions();
        EnsureProxyIsUsedOverHttps(caixaOptions);

        var proxyOptions = caixaOptions.Proxy;
        var routes = CaixaHttpHandlerFactory.BuildRoutes(proxyOptions);
        var timeout = TimeSpan.FromSeconds(Math.Max(1, proxyOptions.TimeoutSeconds));

        foreach (var route in routes)
        {
            services
                .AddHttpClient(route.ClientName, client => client.Timeout = timeout)
                .ConfigurePrimaryHttpMessageHandler(() => CaixaHttpHandlerFactory.Create(route.ProxyAddress, proxyOptions));
        }

        services.AddSingleton(new CaixaProxyRegistry(
            routes,
            TimeSpan.FromSeconds(Math.Max(0, proxyOptions.FailureCooldownSeconds))));
        services.AddScoped<ICaixaLotteryClient, CaixaLotteryClient>();

        return services;
    }

    // Impede sair por proxy sem TLS: em HTTPS o proxy so encaminha o tunel e nao consegue alterar o JSON.
    private static void EnsureProxyIsUsedOverHttps(CaixaLotteryOptions caixaOptions)
    {
        if (!caixaOptions.Proxy.Enabled || caixaOptions.Proxy.ResolveAddresses().Count == 0)
        {
            return;
        }

        var baseUrl = string.IsNullOrWhiteSpace(caixaOptions.BaseUrl)
            ? CaixaLotteryOptions.DefaultBaseUrl
            : caixaOptions.BaseUrl;

        if (!baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "'Caixa:BaseUrl' precisa usar https quando 'Caixa:Proxy' esta habilitado. " +
                "Sem TLS fim a fim, o proxy consegue ler e alterar os resultados dos concursos.");
        }
    }
}
