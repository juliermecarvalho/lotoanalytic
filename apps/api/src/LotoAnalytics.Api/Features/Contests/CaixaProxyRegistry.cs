using System.Collections.Concurrent;

namespace LotoAnalytics.Api.Features.Contests;

public sealed record CaixaHttpRoute(string ClientName, string? ProxyAddress)
{
    public bool UsesProxy => ProxyAddress is not null;

    public string Description => ProxyAddress ?? "conexao direta";
}

public sealed class CaixaProxyRegistry(IReadOnlyList<CaixaHttpRoute> routes, TimeSpan failureCooldown)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> penalizedUntil = new();

    public IReadOnlyList<CaixaHttpRoute> Routes { get; } = routes;

    // Lista as rotas na ordem configurada, deixando por ultimo as que falharam ha pouco tempo.
    public IReadOnlyList<CaixaHttpRoute> GetRoutesByPreference(DateTimeOffset now)
    {
        var available = new List<CaixaHttpRoute>();
        var penalized = new List<CaixaHttpRoute>();

        foreach (var route in Routes)
        {
            if (penalizedUntil.TryGetValue(route.ClientName, out var until) && until > now)
            {
                penalized.Add(route);
                continue;
            }

            available.Add(route);
        }

        // As rotas penalizadas continuam no fim da fila para nao zerar as chances quando todas falharam.
        available.AddRange(penalized);
        return available;
    }

    // Marca uma rota como indisponivel por um periodo apos uma falha.
    public void MarkFailure(CaixaHttpRoute route, DateTimeOffset now)
    {
        if (failureCooldown <= TimeSpan.Zero)
        {
            return;
        }

        penalizedUntil[route.ClientName] = now + failureCooldown;
    }

    // Libera imediatamente uma rota que voltou a responder.
    public void MarkSuccess(CaixaHttpRoute route)
    {
        penalizedUntil.TryRemove(route.ClientName, out _);
    }
}
