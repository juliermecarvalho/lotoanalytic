using LotoAnalytics.Api.Features.Contests;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class CaixaLotteryResultJsonSanitizerTests
{
    [Fact]
    public void SanitizeRemovesNullCharactersFromJsonStrings()
    {
        const string rawJson = """
        {
          "numero": 1871,
          "nomeTimeCoracaoMesSorte": "\u0000\u0000",
          "listaDezenas": ["01", "\u000002"]
        }
        """;

        var sanitized = CaixaLotteryResultJsonSanitizer.Sanitize(rawJson);

        sanitized.ShouldNotContain("\\u0000");
        sanitized.ShouldContain("\"nomeTimeCoracaoMesSorte\":\"\"");
        sanitized.ShouldContain("\"listaDezenas\":[\"01\",\"02\"]");
    }
}
