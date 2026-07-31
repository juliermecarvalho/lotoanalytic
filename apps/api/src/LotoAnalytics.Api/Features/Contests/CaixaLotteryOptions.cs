namespace LotoAnalytics.Api.Features.Contests;

public sealed record CaixaLotteryOptions
{
    public const string SectionName = "Caixa";

    public const string DefaultBaseUrl = "https://servicebus3.caixa.gov.br/portaldeloterias/api";

    // Endereco base das consultas de concurso. Permite apontar para um relay com IP brasileiro
    // quando o servidor da aplicacao esta fora do Brasil e recebe 403 do CDN da Caixa.
    public string BaseUrl { get; init; } = DefaultBaseUrl;

    public CaixaProxyOptions Proxy { get; init; } = new();
}

public sealed record CaixaProxyOptions
{
    // Habilita a saida das chamadas da Caixa por um proxy, normalmente com IP brasileiro.
    public bool Enabled { get; init; }

    // Endereco do proxy, aceitando os esquemas http, https, socks4, socks4a e socks5.
    public string? Address { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }
}
