using LotoAnalytics.Api.Common.Auth;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class KeycloakBackchannelDocumentRetrieverTests
{
    [Fact]
    public void RewriteAddressUsesInternalKeycloakOriginForPublicRealmUrls()
    {
        var rewritten = KeycloakBackchannelDocumentRetriever.RewriteAddress(
            "http://localhost:8080/realms/lotoanalytics/protocol/openid-connect/certs",
            "http://localhost:8080/realms/lotoanalytics",
            "http://keycloak:8080/realms/lotoanalytics/.well-known/openid-configuration");

        rewritten.ShouldBe("http://keycloak:8080/realms/lotoanalytics/protocol/openid-connect/certs");
    }

    [Fact]
    public void RewriteAddressKeepsUnrelatedOrigins()
    {
        var rewritten = KeycloakBackchannelDocumentRetriever.RewriteAddress(
            "http://example.local/realms/lotoanalytics/protocol/openid-connect/certs",
            "http://localhost:8080/realms/lotoanalytics",
            "http://keycloak:8080/realms/lotoanalytics/.well-known/openid-configuration");

        rewritten.ShouldBe("http://example.local/realms/lotoanalytics/protocol/openid-connect/certs");
    }
}
