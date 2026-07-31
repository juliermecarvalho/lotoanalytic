using System.Net;
using LotoAnalytics.Api.Features.Contests;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class CaixaHttpHandlerFactoryTests
{
    [Fact]
    public void CreateDoesNotUseProxyWhenDisabled()
    {
        using var handler = (HttpClientHandler)CaixaHttpHandlerFactory.Create(new CaixaProxyOptions());

        handler.UseProxy.ShouldBeTrue();
        handler.Proxy.ShouldBeNull();
    }

    [Fact]
    public void CreateDoesNotUseProxyWhenAddressIsMissing()
    {
        using var handler = (HttpClientHandler)CaixaHttpHandlerFactory.Create(new CaixaProxyOptions
        {
            Enabled = true,
            Address = "   "
        });

        handler.Proxy.ShouldBeNull();
    }

    [Fact]
    public void CreateUsesConfiguredProxyAddress()
    {
        using var handler = (HttpClientHandler)CaixaHttpHandlerFactory.Create(new CaixaProxyOptions
        {
            Enabled = true,
            Address = "http://proxy-br.exemplo.com.br:8080"
        });

        handler.UseProxy.ShouldBeTrue();
        var proxy = handler.Proxy.ShouldBeOfType<WebProxy>();
        proxy.Address.ShouldNotBeNull();
        proxy.Address.ToString().ShouldBe("http://proxy-br.exemplo.com.br:8080/");
        proxy.Credentials.ShouldBeNull();
    }

    [Fact]
    public void CreateAppliesProxyCredentialsWhenUsernameIsInformed()
    {
        using var handler = (HttpClientHandler)CaixaHttpHandlerFactory.Create(new CaixaProxyOptions
        {
            Enabled = true,
            Address = "socks5://proxy-br.exemplo.com.br:1080",
            Username = "loto",
            Password = "segredo"
        });

        var proxy = handler.Proxy.ShouldBeOfType<WebProxy>();
        var credentials = proxy.Credentials.ShouldBeOfType<NetworkCredential>();
        credentials.UserName.ShouldBe("loto");
        credentials.Password.ShouldBe("segredo");
    }
}
