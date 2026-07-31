using LotoAnalytics.Api.Features.Contests;
using LotoAnalytics.Api.Features.FilterStatistics;
using LotoAnalytics.Api.Infrastructure.Database;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class ContestBulkUpdateServiceTests
{
    [Fact]
    public async Task UpdateAllImportsNextContestsUntilCaixaReturnsNotFound()
    {
        var store = Substitute.For<IContestImportStore>();
        var client = Substitute.For<ICaixaLotteryClient>();
        var statisticsRefresh = Substitute.For<IFilterStatisticsRefreshService>();
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
          "numero": 4,
          "tipoJogo": "LOTOFACIL",
          "dataApuracao": "20/07/2026",
          "listaDezenas": ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15"],
          "listaRateioPremio": []
        }
        """;
        Contest? savedContest = null;

        store.ListActiveModesAsync(TestContext.Current.CancellationToken).Returns([mode]);
        store.GetLatestContestNumberAsync(mode.Id, TestContext.Current.CancellationToken).Returns(3);
        store.SaveContestAsync(Arg.Do<Contest>(contest => savedContest = contest), TestContext.Current.CancellationToken)
            .Returns(Task.CompletedTask);
        client.GetContestResultJsonAsync("lotofacil", 4, TestContext.Current.CancellationToken).Returns(json);
        client
            .GetContestResultJsonAsync("lotofacil", 5, TestContext.Current.CancellationToken)
            .Returns(Task.FromException<string>(new CaixaContestNotFoundException("lotofacil", 5)));
        var service = new ContestBulkUpdateService(store, client, statisticsRefresh);

        var result = await service.UpdateAllAsync(
            new ContestBulkUpdateRequest(DelayMilliseconds: 0),
            TestContext.Current.CancellationToken);

        result.TotalImported.ShouldBe(1);
        result.Modes.Single().ModeCode.ShouldBe("lotofacil");
        result.Modes.Single().ImportedContestNumbers.ShouldBe([4]);
        result.Modes.Single().NextContestNumber.ShouldBe(5);
        result.Modes.Single().Status.ShouldBe("atualizado");
        savedContest.ShouldNotBeNull();
        savedContest.Number.ShouldBe(4);

        // A tabela de estatisticas dos filtros deve ser recalculada apos importar novos concursos.
        await statisticsRefresh.Received(1).RefreshAsync("lotofacil", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateAllReportsProgressForEachImportedContestAndCompletedMode()
    {
        var store = Substitute.For<IContestImportStore>();
        var client = Substitute.For<ICaixaLotteryClient>();
        var mode = new LotteryMode
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Code = "lotofacil",
            Name = "Lotofacil",
            CaixaGameType = "LOTOFACIL",
            MainNumbersCount = 15,
            Active = true
        };
        const string jsonContest4 = """
        {
          "numero": 4,
          "tipoJogo": "LOTOFACIL",
          "dataApuracao": "20/07/2026",
          "listaDezenas": ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15"],
          "listaRateioPremio": []
        }
        """;
        const string jsonContest5 = """
        {
          "numero": 5,
          "tipoJogo": "LOTOFACIL",
          "dataApuracao": "21/07/2026",
          "listaDezenas": ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "16"],
          "listaRateioPremio": []
        }
        """;

        store.ListActiveModesAsync(TestContext.Current.CancellationToken).Returns([mode]);
        store.GetLatestContestNumberAsync(mode.Id, TestContext.Current.CancellationToken).Returns(3);
        store.CountContestsAsync(mode.Id, TestContext.Current.CancellationToken).Returns(5);
        store.SaveContestAsync(Arg.Any<Contest>(), TestContext.Current.CancellationToken).Returns(Task.CompletedTask);
        client.GetContestResultJsonAsync("lotofacil", 4, TestContext.Current.CancellationToken).Returns(jsonContest4);
        client.GetContestResultJsonAsync("lotofacil", 5, TestContext.Current.CancellationToken).Returns(jsonContest5);
        client
            .GetContestResultJsonAsync("lotofacil", 6, TestContext.Current.CancellationToken)
            .Returns(Task.FromException<string>(new CaixaContestNotFoundException("lotofacil", 6)));
        var service = new ContestBulkUpdateService(store, client, Substitute.For<IFilterStatisticsRefreshService>());
        var events = new List<ContestBulkUpdateProgress>();

        var result = await service.UpdateAllAsync(
            new ContestBulkUpdateRequest(DelayMilliseconds: 0),
            TestContext.Current.CancellationToken,
            progress =>
            {
                events.Add(progress);
                return Task.CompletedTask;
            });

        result.TotalImported.ShouldBe(2);
        events.Count.ShouldBe(4);

        events[0].Event.ShouldBe("modalidade_iniciada");
        events[0].ModeCode.ShouldBe("lotofacil");
        events[0].ModeIndex.ShouldBe(1);
        events[0].ModeCount.ShouldBe(1);
        events[0].ResumeFromContestNumber.ShouldBe(4);
        events[0].LastSavedContestNumber.ShouldBe(3);

        events[1].Event.ShouldBe("concurso_importado");
        events[1].ContestNumber.ShouldBe(4);
        events[1].ImportedInMode.ShouldBe(1);
        events[1].MainNumbers.ShouldNotBeNull();
        events[1].MainNumbers!.Count.ShouldBe(15);
        events[1].MainNumbers![0].ShouldBe("01");

        events[2].Event.ShouldBe("concurso_importado");
        events[2].ContestNumber.ShouldBe(5);
        events[2].ImportedInMode.ShouldBe(2);

        events[3].Event.ShouldBe("modalidade_concluida");
        events[3].Status.ShouldBe("atualizado");
        events[3].ImportedInMode.ShouldBe(2);
        events[3].NextContestNumber.ShouldBe(6);
        events[3].TotalInDatabase.ShouldBe(5);
    }

    [Fact]
    public async Task UpdateAllReportsModeWithoutNewContests()
    {
        var store = Substitute.For<IContestImportStore>();
        var client = Substitute.For<ICaixaLotteryClient>();
        var statisticsRefresh = Substitute.For<IFilterStatisticsRefreshService>();
        var mode = new LotteryMode
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Code = "quina",
            Name = "Quina",
            CaixaGameType = "QUINA",
            MainNumbersCount = 5,
            Active = true
        };

        store.ListActiveModesAsync(TestContext.Current.CancellationToken).Returns([mode]);
        store.GetLatestContestNumberAsync(mode.Id, TestContext.Current.CancellationToken).Returns(7073);
        client
            .GetContestResultJsonAsync("quina", 7074, TestContext.Current.CancellationToken)
            .Returns(Task.FromException<string>(new CaixaContestNotFoundException("quina", 7074)));
        var service = new ContestBulkUpdateService(store, client, statisticsRefresh);

        var result = await service.UpdateAllAsync(
            new ContestBulkUpdateRequest(DelayMilliseconds: 0),
            TestContext.Current.CancellationToken);

        result.TotalImported.ShouldBe(0);
        result.Modes.Single().Status.ShouldBe("sem_novos_concursos");
        result.Modes.Single().NextContestNumber.ShouldBe(7074);

        // Sem novos concursos nao ha motivo para recalcular as estatisticas.
        await statisticsRefresh.DidNotReceive().RefreshAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAllRetriesTransientCaixaErrorsForSameContest()
    {
        var store = Substitute.For<IContestImportStore>();
        var client = Substitute.For<ICaixaLotteryClient>();
        var mode = new LotteryMode
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Code = "lotofacil",
            Name = "Lotofacil",
            CaixaGameType = "LOTOFACIL",
            MainNumbersCount = 15,
            Active = true
        };
        const string json = """
        {
          "numero": 11,
          "tipoJogo": "LOTOFACIL",
          "dataApuracao": "20/07/2026",
          "listaDezenas": ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15"],
          "listaRateioPremio": []
        }
        """;

        store.ListActiveModesAsync(TestContext.Current.CancellationToken).Returns([mode]);
        store.GetLatestContestNumberAsync(mode.Id, TestContext.Current.CancellationToken).Returns(10);
        store.SaveContestAsync(Arg.Any<Contest>(), TestContext.Current.CancellationToken).Returns(Task.CompletedTask);
        client
            .GetContestResultJsonAsync("lotofacil", 11, TestContext.Current.CancellationToken)
            .Returns(
                Task.FromException<string>(new CaixaTransientApiException("lotofacil", 11, "HTTP 500")),
                Task.FromResult(json));
        client
            .GetContestResultJsonAsync("lotofacil", 12, TestContext.Current.CancellationToken)
            .Returns(Task.FromException<string>(new CaixaContestNotFoundException("lotofacil", 12)));
        var service = new ContestBulkUpdateService(store, client, Substitute.For<IFilterStatisticsRefreshService>());
        var events = new List<ContestBulkUpdateProgress>();

        var result = await service.UpdateAllAsync(
            new ContestBulkUpdateRequest(DelayMilliseconds: 0, ErrorDelayMilliseconds: 0, MaxRetryAttempts: 2),
            TestContext.Current.CancellationToken,
            progress =>
            {
                events.Add(progress);
                return Task.CompletedTask;
            });

        result.TotalImported.ShouldBe(1);
        result.Modes.Single().ImportedContestNumbers.ShouldBe([11]);
        await client.Received(2).GetContestResultJsonAsync("lotofacil", 11, TestContext.Current.CancellationToken);

        // A espera do retry deve ser visivel no progresso para a tela nao parecer travada.
        var retryEvent = events.Single(progress => progress.Event == "tentativa_falhou");
        retryEvent.ContestNumber.ShouldBe(11);
        retryEvent.RetryAttempt.ShouldBe(1);
        retryEvent.RetryDelayMilliseconds.ShouldBe(0);
    }

    [Fact]
    public async Task UpdateAllDoesNotRetryBlockedAccessAndContinuesToNextMode()
    {
        var store = Substitute.For<IContestImportStore>();
        var client = Substitute.For<ICaixaLotteryClient>();
        var blockedMode = new LotteryMode
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Code = "lotofacil",
            Name = "Lotofacil",
            CaixaGameType = "LOTOFACIL",
            MainNumbersCount = 15,
            Active = true
        };
        var nextMode = new LotteryMode
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Code = "quina",
            Name = "Quina",
            CaixaGameType = "QUINA",
            MainNumbersCount = 5,
            Active = true
        };

        store.ListActiveModesAsync(TestContext.Current.CancellationToken).Returns([blockedMode, nextMode]);
        store.GetLatestContestNumberAsync(blockedMode.Id, TestContext.Current.CancellationToken).Returns(10);
        store.GetLatestContestNumberAsync(nextMode.Id, TestContext.Current.CancellationToken).Returns(7073);
        client
            .GetContestResultJsonAsync("lotofacil", 11, TestContext.Current.CancellationToken)
            .Returns(Task.FromException<string>(
                new CaixaAccessBlockedException("lotofacil", 11, "https://servicebus3.caixa.gov.br/portaldeloterias/api/lotofacil/11")));
        client
            .GetContestResultJsonAsync("quina", 7074, TestContext.Current.CancellationToken)
            .Returns(Task.FromException<string>(new CaixaContestNotFoundException("quina", 7074)));
        var service = new ContestBulkUpdateService(store, client, Substitute.For<IFilterStatisticsRefreshService>());

        var result = await service.UpdateAllAsync(
            new ContestBulkUpdateRequest(DelayMilliseconds: 0, ErrorDelayMilliseconds: 0),
            TestContext.Current.CancellationToken);

        // O bloqueio nao pode virar retry infinito: a chamada acontece uma unica vez.
        await client.Received(1).GetContestResultJsonAsync("lotofacil", 11, TestContext.Current.CancellationToken);
        result.Modes[0].Status.ShouldBe("falhou");
        result.Modes[0].Error.ShouldNotBeNull();
        result.Modes[0].Error!.ShouldContain("403");

        // As demais modalidades continuam sendo processadas mesmo com a primeira bloqueada.
        result.Modes.Count.ShouldBe(2);
        result.Modes[1].ModeCode.ShouldBe("quina");
        result.Modes[1].Status.ShouldBe("sem_novos_concursos");
    }
}
