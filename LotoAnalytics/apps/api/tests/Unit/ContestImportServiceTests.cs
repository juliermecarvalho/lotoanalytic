using LotoAnalytics.Api.Features.Contests;
using LotoAnalytics.Api.Features.FilterStatistics;
using LotoAnalytics.Api.Infrastructure.Database;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class ContestImportServiceTests
{
    [Fact]
    public async Task ImportContestFetchesParsesAndSavesContest()
    {
        var store = Substitute.For<IContestImportStore>();
        var client = Substitute.For<ICaixaLotteryClient>();
        var mode = new LotteryMode
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Code = "lotofacil",
            Name = "Lotofacil",
            CaixaGameType = "LOTOFACIL",
            MainNumbersCount = 15,
            Active = true
        };
        const string json = """
        {
          "numero": 3733,
          "tipoJogo": "LOTOFACIL",
          "dataApuracao": "11/07/2026",
          "listaDezenas": ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "13", "17", "19", "23"],
          "dezenasSorteadasOrdemSorteio": ["01", "10", "23"],
          "listaRateioPremio": [
            { "faixa": 1, "descricaoFaixa": "15 acertos", "numeroDeGanhadores": 2, "valorPremio": 724525.32 }
          ],
          "listaMunicipioUFGanhadores": [
            { "municipio": "SANTOS", "uf": "SP", "ganhadores": 1 }
          ]
        }
        """;
        Contest? savedContest = null;

        store.FindModeByCodeAsync("lotofacil", TestContext.Current.CancellationToken).Returns(mode);
        client.GetContestResultJsonAsync("lotofacil", 3733, TestContext.Current.CancellationToken).Returns(json);
        store.SaveContestAsync(Arg.Do<Contest>(contest => savedContest = contest), TestContext.Current.CancellationToken)
            .Returns(Task.CompletedTask);
        var statisticsRefresh = Substitute.For<IFilterStatisticsRefreshService>();
        var service = new ContestImportService(store, client, statisticsRefresh);

        var result = await service.ImportContestAsync("lotofacil", 3733, TestContext.Current.CancellationToken);

        result.ModeCode.ShouldBe("lotofacil");
        result.ContestNumber.ShouldBe(3733);
        result.MainNumbersCount.ShouldBe(15);
        result.PrizeTiersCount.ShouldBe(1);
        savedContest.ShouldNotBeNull();
        savedContest.RawResultJson.ShouldContain("\"numero\":3733");
        savedContest.WinnerCities.Single().City.ShouldBe("SANTOS");

        // Importar um concurso tambem recalcula a tabela de estatisticas dos filtros.
        await statisticsRefresh.Received(1).RefreshAsync("lotofacil", TestContext.Current.CancellationToken);
    }
}
