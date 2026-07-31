using System.Net;
using LotoAnalytics.Api.Features.Contests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class CaixaHttpHandlerFactoryTests
{
    [Fact]
    public void CreateDoesNotUseProxyWhenAddressIsMissing()
    {
        using var handler = (HttpClientHandler)CaixaHttpHandlerFactory.Create("   ", new CaixaProxyOptions());

        handler.Proxy.ShouldBeNull();
    }

    [Fact]
    public void CreateUsesConfiguredProxyAddress()
    {
        using var handler = (HttpClientHandler)CaixaHttpHandlerFactory.Create(
            "http://proxy-br.exemplo.com.br:8080",
            new CaixaProxyOptions());

        handler.UseProxy.ShouldBeTrue();
        var proxy = handler.Proxy.ShouldBeOfType<WebProxy>();
        proxy.Address!.ToString().ShouldBe("http://proxy-br.exemplo.com.br:8080/");
        proxy.Credentials.ShouldBeNull();
    }

    [Fact]
    public void CreateAppliesProxyCredentialsWhenUsernameIsInformed()
    {
        using var handler = (HttpClientHandler)CaixaHttpHandlerFactory.Create(
            "socks5://proxy-br.exemplo.com.br:1080",
            new CaixaProxyOptions { Username = "loto", Password = "segredo" });

        var proxy = handler.Proxy.ShouldBeOfType<WebProxy>();
        var credentials = proxy.Credentials.ShouldBeOfType<NetworkCredential>();
        credentials.UserName.ShouldBe("loto");
        credentials.Password.ShouldBe("segredo");
    }

    [Fact]
    public void BuildRoutesReturnsDirectRouteWhenProxyIsDisabled()
    {
        var routes = CaixaHttpHandlerFactory.BuildRoutes(new CaixaProxyOptions
        {
            Enabled = false,
            Addresses = ["http://proxy-br.exemplo.com.br:8080"]
        });

        routes.Single().UsesProxy.ShouldBeFalse();
        routes.Single().ClientName.ShouldBe(CaixaHttpHandlerFactory.DirectClientName);
    }

    [Fact]
    public void BuildRoutesCreatesOneRoutePerProxyPreservingOrder()
    {
        var routes = CaixaHttpHandlerFactory.BuildRoutes(new CaixaProxyOptions
        {
            Enabled = true,
            Address = "http://primeiro.exemplo:8080",
            Addresses = ["http://segundo.exemplo:8080", "http://terceiro.exemplo:8080"]
        });

        routes.Count.ShouldBe(3);
        routes[0].ProxyAddress.ShouldBe("http://primeiro.exemplo:8080");
        routes[1].ProxyAddress.ShouldBe("http://segundo.exemplo:8080");
        routes[2].ProxyAddress.ShouldBe("http://terceiro.exemplo:8080");
        routes.Select(route => route.ClientName).Distinct().Count().ShouldBe(3);
    }

    [Fact]
    public void ResolveAddressesRemovesBlankAndDuplicatedEntries()
    {
        var addresses = new CaixaProxyOptions
        {
            Address = "http://um.exemplo:8080",
            Addresses = ["  ", "http://um.exemplo:8080", " http://dois.exemplo:8080 "]
        }.ResolveAddresses();

        addresses.ShouldBe(["http://um.exemplo:8080", "http://dois.exemplo:8080"]);
    }

    [Fact]
    public void ResolveAddressesAcceptsSeparatedAddressList()
    {
        var addresses = new CaixaProxyOptions
        {
            AddressList = "http://um.exemplo:8080, socks5://dois.exemplo:1080;http://tres.exemplo:3128"
        }.ResolveAddresses();

        addresses.ShouldBe(["http://um.exemplo:8080", "socks5://dois.exemplo:1080", "http://tres.exemplo:3128"]);
    }

    [Fact]
    public void ResolveAddressesMergesAddressListWithIndexedAddresses()
    {
        var addresses = new CaixaProxyOptions
        {
            Addresses = ["http://um.exemplo:8080"],
            AddressList = "http://um.exemplo:8080,http://dois.exemplo:8080"
        }.ResolveAddresses();

        addresses.ShouldBe(["http://um.exemplo:8080", "http://dois.exemplo:8080"]);
    }

    [Fact]
    public void AddCaixaLotteryClientBuildsOneRoutePerAddressInTheList()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Caixa:Proxy:Enabled"] = "true",
                ["Caixa:Proxy:AddressList"] = "http://um.exemplo:8080,http://dois.exemplo:8080,http://tres.exemplo:8080"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddLogging();

        services.AddCaixaLotteryClient(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<CaixaProxyRegistry>().Routes.Count.ShouldBe(3);
    }

    [Fact]
    public void AddCaixaLotteryClientRejectsProxyWithoutHttpsBaseUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Caixa:BaseUrl"] = "http://relay-inseguro.exemplo/api",
                ["Caixa:Proxy:Enabled"] = "true",
                ["Caixa:Proxy:Addresses:0"] = "http://proxy-br.exemplo:8080"
            })
            .Build();

        // Sem TLS fim a fim o proxy conseguiria alterar os numeros sorteados.
        var exception = Should.Throw<InvalidOperationException>(
            () => new ServiceCollection().AddCaixaLotteryClient(configuration));

        exception.Message.ShouldContain("https");
    }

    [Fact]
    public void AddCaixaLotteryClientAcceptsProxyWithHttpsBaseUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Caixa:Proxy:Enabled"] = "true",
                ["Caixa:Proxy:Addresses:0"] = "http://proxy-br.exemplo:8080",
                ["Caixa:Proxy:Addresses:1"] = "http://outro-proxy-br.exemplo:3128"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddLogging();

        services.AddCaixaLotteryClient(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<CaixaProxyRegistry>().Routes.Count.ShouldBe(2);
        provider.GetRequiredService<ICaixaLotteryClient>().ShouldBeOfType<CaixaLotteryClient>();
    }
}
