using LotoAnalytics.Api.Infrastructure.Database;

namespace LotoAnalytics.Api.Features.Contests;

public static class ContestImportMapper
{
    // Converte o resultado normalizado da Caixa no agregado de concurso persistivel.
    public static Contest Map(LotteryMode lotteryMode, CaixaLotteryResult result, string rawResultJson)
    {
        var contest = new Contest
        {
            LotteryModeId = lotteryMode.Id,
            Number = result.ContestNumber,
            DrawDate = result.DrawDate,
            RawResultJson = rawResultJson,
            Numbers = [],
            PrizeTiers = [],
            WinnerCities = []
        };

        contest.Numbers.AddRange(MapNumbers(result.MainNumbers, "principal"));
        contest.Numbers.AddRange(MapNumbers(result.SecondDrawNumbers, "segundo_sorteio"));
        contest.Numbers.AddRange(MapNumbers(result.DrawOrderNumbers, "ordem_sorteio"));
        contest.Numbers.AddRange(MapNumbers(result.Trevos, "trevo"));
        contest.PrizeTiers.AddRange(MapPrizeTiers(result.PrizeTiers));
        contest.WinnerCities.AddRange(MapWinnerCities(result.WinnerCities));

        return contest;
    }

    // Converte uma lista de dezenas para entidades com tipo e posicao preservados.
    private static IEnumerable<ContestNumber> MapNumbers(IReadOnlyList<string> numbers, string numberType)
    {
        return numbers.Select((number, index) => new ContestNumber
        {
            NumberType = numberType,
            Position = index + 1,
            Value = number,
            NumericValue = int.TryParse(number, out var numericValue) ? numericValue : null
        });
    }

    // Converte as faixas de premio para entidades filhas do concurso.
    private static IEnumerable<ContestPrizeTier> MapPrizeTiers(IReadOnlyList<PrizeTier> prizeTiers)
    {
        return prizeTiers.Select(prizeTier => new ContestPrizeTier
        {
            Tier = prizeTier.Tier,
            Description = prizeTier.Description,
            WinnersCount = prizeTier.WinnersCount,
            PrizeValue = prizeTier.PrizeValue
        });
    }

    // Converte os municipios ganhadores para entidades filhas do concurso.
    private static IEnumerable<ContestWinnerCity> MapWinnerCities(IReadOnlyList<WinnerCity> winnerCities)
    {
        return winnerCities.Select(winnerCity => new ContestWinnerCity
        {
            City = winnerCity.City,
            State = winnerCity.State,
            WinnersCount = winnerCity.WinnersCount
        });
    }
}
