using LotoAnalytics.Api.Features.Contests;
using LotoAnalytics.Api.Infrastructure.Database;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class ContestImportMapperTests
{
    [Fact]
    public void MapCreatesContestWithNumbersAndPrizeTiers()
    {
        var modeId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var mode = new LotteryMode
        {
            Id = modeId,
            Code = "maismilionaria",
            Name = "+Milionaria",
            CaixaGameType = "MAIS_MILIONARIA",
            MainNumbersCount = 6,
            Active = true
        };
        var result = new CaixaLotteryResult(
            ContestNumber: 371,
            CaixaGameType: "MAIS_MILIONARIA",
            DrawDate: new DateOnly(2026, 7, 11),
            MainNumbers: ["05", "13", "19", "35", "49", "50"],
            SecondDrawNumbers: [],
            DrawOrderNumbers: ["35", "49", "05", "50", "19", "13", "2", "3"],
            Trevos: ["2", "3"],
            PrizeTiers: [new PrizeTier(1, "6 acertos + 2 trevos", 0, 0m)],
            WinnerCities: [new WinnerCity("SAO PAULO", "SP", 2)]);

        var contest = ContestImportMapper.Map(mode, result, """{"numero":371}""");

        contest.LotteryModeId.ShouldBe(modeId);
        contest.Number.ShouldBe(371);
        contest.DrawDate.ShouldBe(new DateOnly(2026, 7, 11));
        contest.RawResultJson.ShouldBe("""{"numero":371}""");
        contest.Numbers.Select(number => (number.NumberType, number.Position, number.Value)).ShouldBe(
        [
            ("principal", 1, "05"),
            ("principal", 2, "13"),
            ("principal", 3, "19"),
            ("principal", 4, "35"),
            ("principal", 5, "49"),
            ("principal", 6, "50"),
            ("ordem_sorteio", 1, "35"),
            ("ordem_sorteio", 2, "49"),
            ("ordem_sorteio", 3, "05"),
            ("ordem_sorteio", 4, "50"),
            ("ordem_sorteio", 5, "19"),
            ("ordem_sorteio", 6, "13"),
            ("ordem_sorteio", 7, "2"),
            ("ordem_sorteio", 8, "3"),
            ("trevo", 1, "2"),
            ("trevo", 2, "3")
        ]);
        contest.PrizeTiers.Single().Description.ShouldBe("6 acertos + 2 trevos");
        contest.WinnerCities.Single().City.ShouldBe("SAO PAULO");
        contest.WinnerCities.Single().State.ShouldBe("SP");
        contest.WinnerCities.Single().WinnersCount.ShouldBe(2);
    }
}
