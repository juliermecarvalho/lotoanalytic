using LotoAnalytics.Api.Features.GameChecking;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class LotofacilGameCheckerTests
{
    [Fact]
    public void CheckReturnsHitsAndAwardSummaryForUserGames()
    {
        var drawnNumbers = new[]
        {
            "01", "02", "03", "04", "05",
            "06", "07", "08", "09", "10",
            "11", "12", "13", "14", "15"
        };
        var games = new[]
        {
            new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15" },
            new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "16" },
            new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "16", "17", "18", "19" },
            new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "16", "17", "18", "19", "20" }
        };

        var result = LotofacilGameChecker.Check(drawnNumbers, games);

        result.Games.Select(game => game.HitCount).ShouldBe([15, 14, 11, 10]);
        result.Games[1].MatchedNumbers.ShouldBe(["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14"]);
        result.Games[2].IsAwarded.ShouldBeTrue();
        result.Games[3].IsAwarded.ShouldBeFalse();
        result.AwardSummary.ShouldContainKeyAndValue(15, 1);
        result.AwardSummary.ShouldContainKeyAndValue(14, 1);
        result.AwardSummary.ShouldContainKeyAndValue(13, 0);
        result.AwardSummary.ShouldContainKeyAndValue(12, 0);
        result.AwardSummary.ShouldContainKeyAndValue(11, 1);
    }
}
