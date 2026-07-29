using LotoAnalytics.Api.Features.Contests;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class CaixaLotteryResultParserTests
{
    [Fact]
    public void ParseKeepsSecondDrawNumbersForDuplaSena()
    {
        const string json = """
        {
          "numero": 2981,
          "tipoJogo": "DUPLA_SENA",
          "dataApuracao": "10/07/2026",
          "listaDezenas": ["04", "05", "14", "18", "42", "44"],
          "listaDezenasSegundoSorteio": ["01", "02", "13", "19", "25", "33"],
          "dezenasSorteadasOrdemSorteio": ["42", "44", "18", "05", "14", "04", "25", "13", "02", "33", "19", "01"],
          "listaRateioPremio": [
            { "faixa": 1, "descricaoFaixa": "6 acertos", "numeroDeGanhadores": 0, "valorPremio": 0.0 },
            { "faixa": 2, "descricaoFaixa": "5 acertos", "numeroDeGanhadores": 7, "valorPremio": 5897.12 }
          ],
          "listaMunicipioUFGanhadores": []
        }
        """;

        var result = CaixaLotteryResultParser.Parse(json);

        result.ContestNumber.ShouldBe(2981);
        result.CaixaGameType.ShouldBe("DUPLA_SENA");
        result.MainNumbers.ShouldBe(["04", "05", "14", "18", "42", "44"]);
        result.SecondDrawNumbers.ShouldBe(["01", "02", "13", "19", "25", "33"]);
        result.DrawOrderNumbers.Count.ShouldBe(12);
        result.PrizeTiers.Select(tier => tier.Description).ShouldBe(["6 acertos", "5 acertos"]);
    }

    [Fact]
    public void ParseKeepsTrevosForMaisMilionaria()
    {
        const string json = """
        {
          "numero": 371,
          "tipoJogo": "MAIS_MILIONARIA",
          "dataApuracao": "11/07/2026",
          "listaDezenas": ["05", "13", "19", "35", "49", "50"],
          "trevosSorteados": ["2", "3"],
          "dezenasSorteadasOrdemSorteio": ["35", "49", "05", "50", "19", "13", "2", "3"],
          "listaRateioPremio": [
            { "faixa": 1, "descricaoFaixa": "6 acertos + 2 trevos", "numeroDeGanhadores": 0, "valorPremio": 0.0 }
          ],
          "listaMunicipioUFGanhadores": []
        }
        """;

        var result = CaixaLotteryResultParser.Parse(json);

        result.ContestNumber.ShouldBe(371);
        result.CaixaGameType.ShouldBe("MAIS_MILIONARIA");
        result.MainNumbers.ShouldBe(["05", "13", "19", "35", "49", "50"]);
        result.Trevos.ShouldBe(["2", "3"]);
        result.DrawOrderNumbers.ShouldBe(["35", "49", "05", "50", "19", "13", "2", "3"]);
        result.PrizeTiers.Single().Description.ShouldBe("6 acertos + 2 trevos");
    }
}
