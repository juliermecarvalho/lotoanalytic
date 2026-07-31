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
    // Habilita a saida das chamadas da Caixa por proxy, normalmente com IP brasileiro.
    public bool Enabled { get; init; }

    // Proxy unico, atalho equivalente a informar um endereco em Addresses.
    public string? Address { get; init; }

    // Lista de proxies tentados em ordem. Proxies publicos gratuitos caem com frequencia,
    // entao a lista permite que a importacao continue quando um deles para de responder.
    public IReadOnlyList<string> Addresses { get; init; } = [];

    // Mesma lista em uma unica string separada por virgula, ponto e virgula ou espaco.
    // Existe porque deploys por variavel de ambiente ficam impraticaveis com dezenas de chaves indexadas.
    public string? AddressList { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    // Tempo maximo de cada tentativa. Proxies publicos costumam ficar lentos ou pendurados.
    public int TimeoutSeconds { get; init; } = 15;

    // Periodo em que um proxy que falhou deixa de ser tentado, evitando repetir enderecos mortos.
    // Proxies publicos oscilam em minutos, entao a quarentena e curta.
    public int FailureCooldownSeconds { get; init; } = 120;

    // Reune Address, Addresses e AddressList, preservando a ordem e ignorando duplicados.
    public IReadOnlyList<string> ResolveAddresses()
    {
        var addresses = new List<string>();
        var separatedAddresses = (AddressList ?? string.Empty)
            .Split([',', ';', ' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var address in new[] { Address }.Concat(Addresses).Concat(separatedAddresses))
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                continue;
            }

            var normalized = address.Trim();
            if (!addresses.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                addresses.Add(normalized);
            }
        }

        return addresses;
    }
}
