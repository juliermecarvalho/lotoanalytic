using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace LotoAnalytics.Api.Common.Auth;

public static class KeycloakAuthenticationExtensions
{
    // Configura autenticacao JWT Bearer usando um realm Keycloak ja existente.
    public static IServiceCollection AddKeycloakJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>() ?? new KeycloakOptions();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtOptions =>
            {
                jwtOptions.Authority = options.Authority;
                jwtOptions.Audience = options.Audience;
                if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
                {
                    jwtOptions.MetadataAddress = options.MetadataAddress;
                }

                jwtOptions.RequireHttpsMetadata = options.RequireHttpsMetadata;
                if (!string.IsNullOrWhiteSpace(options.MetadataAddress))
                {
                    jwtOptions.ConfigurationManager = CreateConfigurationManager(options);
                }

                jwtOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = !string.IsNullOrWhiteSpace(options.Audience),
                    NameClaimType = options.UsernameClaim,
                    RoleClaimType = ClaimTypes.Role
                };
                jwtOptions.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        AddKeycloakRoles(context.Principal);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    // Cria o gerenciador OIDC usando endereco interno para chamadas de backchannel.
    private static ConfigurationManager<OpenIdConnectConfiguration> CreateConfigurationManager(KeycloakOptions options)
    {
        var retriever = new KeycloakBackchannelDocumentRetriever(
            new HttpDocumentRetriever { RequireHttps = options.RequireHttpsMetadata },
            options.Authority,
            options.MetadataAddress);

        return new ConfigurationManager<OpenIdConnectConfiguration>(
            options.MetadataAddress!,
            new OpenIdConnectConfigurationRetriever(),
            retriever);
    }

    // Copia papeis do Keycloak para claims padrao usadas pelo ASP.NET Core.
    private static void AddKeycloakRoles(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        foreach (var role in ReadRealmRoles(identity))
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }
    }

    // Le os papeis do campo realm_access.roles emitido pelo Keycloak.
    private static IEnumerable<string> ReadRealmRoles(ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return [];
        }

        using var document = JsonDocument.Parse(realmAccess);
        if (!document.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return roles
            .EnumerateArray()
            .Where(role => role.ValueKind == JsonValueKind.String)
            .Select(role => role.GetString())
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role!)
            .ToArray();
    }
}

public sealed record KeycloakOptions
{
    public const string SectionName = "Keycloak";

    public string Authority { get; init; } = "http://localhost:8080/realms/lotoanalytics";

    public string Audience { get; init; } = "lotoanalytics-api";

    public string? MetadataAddress { get; init; }

    public bool RequireHttpsMetadata { get; init; }

    public string UsernameClaim { get; init; } = "preferred_username";
}

public sealed class KeycloakBackchannelDocumentRetriever(
    IDocumentRetriever innerRetriever,
    string publicAuthority,
    string? metadataAddress) : IDocumentRetriever
{
    // Busca documentos OIDC reescrevendo URLs publicas para o host interno do Docker.
    public async Task<string> GetDocumentAsync(string address, CancellationToken cancel)
    {
        var rewrittenAddress = RewriteAddress(address, publicAuthority, metadataAddress);
        return await innerRetriever.GetDocumentAsync(rewrittenAddress, cancel);
    }

    // Reescreve apenas URLs que usam a origem publica do Keycloak.
    public static string RewriteAddress(string address, string publicAuthority, string? metadataAddress)
    {
        if (string.IsNullOrWhiteSpace(metadataAddress)
            || !Uri.TryCreate(address, UriKind.Absolute, out var requestedUri)
            || !Uri.TryCreate(publicAuthority, UriKind.Absolute, out var publicUri)
            || !Uri.TryCreate(metadataAddress, UriKind.Absolute, out var metadataUri))
        {
            return address;
        }

        if (!OriginsMatch(requestedUri, publicUri))
        {
            return address;
        }

        var builder = new UriBuilder(requestedUri)
        {
            Scheme = metadataUri.Scheme,
            Host = metadataUri.Host,
            Port = metadataUri.Port
        };

        return builder.Uri.ToString();
    }

    // Compara protocolo, host e porta das URLs.
    private static bool OriginsMatch(Uri left, Uri right)
    {
        return string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
            && left.Port == right.Port;
    }
}
