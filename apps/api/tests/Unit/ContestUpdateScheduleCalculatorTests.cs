using LotoAnalytics.Api.Features.Contests;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class ContestUpdateScheduleCalculatorTests
{
    [Fact]
    public void CalculateDelayUntilNextRunReturnsTodayWhenDailyTimeIsStillAhead()
    {
        var utcNow = new DateTimeOffset(2026, 7, 25, 3, 30, 0, TimeSpan.Zero);

        var delay = ContestUpdateScheduleCalculator.CalculateDelayUntilNextRun(
            utcNow,
            new TimeOnly(1, 0),
            "America/Sao_Paulo");

        delay.ShouldBe(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void CalculateDelayUntilNextRunReturnsTomorrowWhenDailyTimeAlreadyPassed()
    {
        var utcNow = new DateTimeOffset(2026, 7, 25, 5, 0, 0, TimeSpan.Zero);

        var delay = ContestUpdateScheduleCalculator.CalculateDelayUntilNextRun(
            utcNow,
            new TimeOnly(1, 0),
            "America/Sao_Paulo");

        delay.ShouldBe(TimeSpan.FromHours(23));
    }
}
