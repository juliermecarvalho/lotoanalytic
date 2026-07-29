using LotoAnalytics.Api.Features.FilterStatistics;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Features.Contests;

public interface IContestImportStore
{
    Task<LotteryMode?> FindModeByCodeAsync(string modeCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<LotteryMode>> ListActiveModesAsync(CancellationToken cancellationToken);

    Task<int?> GetLatestContestNumberAsync(Guid lotteryModeId, CancellationToken cancellationToken);

    Task<int> CountContestsAsync(Guid lotteryModeId, CancellationToken cancellationToken);

    Task SaveContestAsync(Contest contest, CancellationToken cancellationToken);
}

public interface IContestImportService
{
    Task<ContestImportResult> ImportContestAsync(string modeCode, int contestNumber, CancellationToken cancellationToken);
}

public sealed class ContestImportService(
    IContestImportStore store,
    ICaixaLotteryClient client,
    IFilterStatisticsRefreshService statisticsRefresh) : IContestImportService
{
    // Importa um concurso oficial da Caixa e persiste os dados normalizados.
    public async Task<ContestImportResult> ImportContestAsync(
        string modeCode,
        int contestNumber,
        CancellationToken cancellationToken)
    {
        var lotteryMode = await store.FindModeByCodeAsync(modeCode, cancellationToken)
            ?? throw new InvalidOperationException($"Modalidade nao encontrada: {modeCode}");
        var rawJson = CaixaLotteryResultJsonSanitizer.Sanitize(
            await client.GetContestResultJsonAsync(modeCode, contestNumber, cancellationToken));
        var parsedResult = CaixaLotteryResultParser.Parse(rawJson);
        var contest = ContestImportMapper.Map(lotteryMode, parsedResult, rawJson);

        await store.SaveContestAsync(contest, cancellationToken);
        await statisticsRefresh.RefreshAsync(lotteryMode.Code, cancellationToken);

        return new ContestImportResult(
            ModeCode: lotteryMode.Code,
            ContestNumber: contest.Number,
            MainNumbersCount: parsedResult.MainNumbers.Count,
            PrizeTiersCount: parsedResult.PrizeTiers.Count);
    }
}

public sealed class EfContestImportStore(LotoAnalyticsDbContext dbContext) : IContestImportStore
{
    // Localiza a modalidade ativa que sera usada como base da importacao.
    public Task<LotteryMode?> FindModeByCodeAsync(string modeCode, CancellationToken cancellationToken)
    {
        return dbContext.LotteryModes
            .SingleOrDefaultAsync(mode => mode.Code == modeCode && mode.Active, cancellationToken);
    }

    // Lista as modalidades ativas que participam da atualizacao geral.
    public async Task<IReadOnlyList<LotteryMode>> ListActiveModesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.LotteryModes
            .AsNoTracking()
            .Where(mode => mode.Active)
            .OrderBy(mode => mode.Id)
            .ToListAsync(cancellationToken);
    }

    // Consulta o maior numero de concurso salvo para uma modalidade.
    public async Task<int?> GetLatestContestNumberAsync(Guid lotteryModeId, CancellationToken cancellationToken)
    {
        return await dbContext.Contests
            .AsNoTracking()
            .Where(contest => contest.LotteryModeId == lotteryModeId)
            .MaxAsync(contest => (int?)contest.Number, cancellationToken);
    }

    // Conta quantos concursos ja estao salvos para uma modalidade.
    public Task<int> CountContestsAsync(Guid lotteryModeId, CancellationToken cancellationToken)
    {
        return dbContext.Contests
            .AsNoTracking()
            .CountAsync(contest => contest.LotteryModeId == lotteryModeId, cancellationToken);
    }

    // Salva o concurso normalizado, substituindo concurso anterior da mesma modalidade e numero.
    public async Task SaveContestAsync(Contest contest, CancellationToken cancellationToken)
    {
        var existingContest = await dbContext.Contests
            .Include(existing => existing.Numbers)
            .Include(existing => existing.PrizeTiers)
            .Include(existing => existing.WinnerCities)
            .SingleOrDefaultAsync(
                existing => existing.LotteryModeId == contest.LotteryModeId && existing.Number == contest.Number,
                cancellationToken);

        if (existingContest is not null)
        {
            dbContext.ContestNumbers.RemoveRange(existingContest.Numbers);
            dbContext.ContestPrizeTiers.RemoveRange(existingContest.PrizeTiers);
            dbContext.ContestWinnerCities.RemoveRange(existingContest.WinnerCities);
            dbContext.Contests.Remove(existingContest);
        }

        dbContext.Contests.Add(contest);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed record ContestImportResult(
    string ModeCode,
    int ContestNumber,
    int MainNumbersCount,
    int PrizeTiersCount);

// Callback assincrono chamado ao iniciar cada modalidade, a cada concurso importado e ao concluir cada modalidade.
public delegate Task ContestBulkUpdateProgressCallback(ContestBulkUpdateProgress progress);

public sealed record ContestBulkUpdateProgress(
    string Event,
    string ModeCode,
    string ModeName,
    int ModeIndex,
    int ModeCount,
    int? ContestNumber,
    IReadOnlyList<string>? MainNumbers,
    int ImportedInMode,
    int? ResumeFromContestNumber,
    int? LastSavedContestNumber,
    int? NextContestNumber,
    int? TotalInDatabase,
    string? Status,
    string? Error,
    int? RetryAttempt = null,
    int? RetryDelayMilliseconds = null);

public interface IContestBulkUpdateService
{
    Task<ContestBulkUpdateResult> UpdateAllAsync(
        ContestBulkUpdateRequest request,
        CancellationToken cancellationToken,
        ContestBulkUpdateProgressCallback? onProgress = null);
}

public sealed class ContestBulkUpdateService(
    IContestImportStore store,
    ICaixaLotteryClient client,
    IFilterStatisticsRefreshService statisticsRefresh) : IContestBulkUpdateService
{
    // Atualiza todas as modalidades ativas retomando do ultimo concurso salvo.
    public async Task<ContestBulkUpdateResult> UpdateAllAsync(
        ContestBulkUpdateRequest request,
        CancellationToken cancellationToken,
        ContestBulkUpdateProgressCallback? onProgress = null)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var modes = await store.ListActiveModesAsync(cancellationToken);
        var modeResults = new List<ContestBulkUpdateModeResult>();

        for (var modeIndex = 0; modeIndex < modes.Count; modeIndex++)
        {
            var mode = modes[modeIndex];
            var modeResult = await UpdateModeAsync(
                mode,
                modeIndex + 1,
                modes.Count,
                request,
                cancellationToken,
                onProgress);
            modeResults.Add(modeResult);

            if (onProgress is not null)
            {
                var totalInDatabase = await store.CountContestsAsync(mode.Id, cancellationToken);
                await onProgress(new ContestBulkUpdateProgress(
                    Event: "modalidade_concluida",
                    ModeCode: modeResult.ModeCode,
                    ModeName: modeResult.ModeName,
                    ModeIndex: modeIndex + 1,
                    ModeCount: modes.Count,
                    ContestNumber: null,
                    MainNumbers: null,
                    ImportedInMode: modeResult.ImportedContestNumbers.Count,
                    ResumeFromContestNumber: null,
                    LastSavedContestNumber: null,
                    NextContestNumber: modeResult.NextContestNumber,
                    TotalInDatabase: totalInDatabase,
                    Status: modeResult.Status,
                    Error: modeResult.Error));
            }
        }

        // Recalcula as estatisticas de filtro apenas das modalidades que receberam novos concursos.
        foreach (var modeResult in modeResults.Where(result => result.ImportedContestNumbers.Count > 0))
        {
            await statisticsRefresh.RefreshAsync(modeResult.ModeCode, cancellationToken);
        }

        return new ContestBulkUpdateResult(
            StartedAt: startedAt,
            FinishedAt: DateTimeOffset.UtcNow,
            Modes: modeResults);
    }

    // Atualiza uma modalidade ate encontrar fim de sorteios ou limite informado.
    private async Task<ContestBulkUpdateModeResult> UpdateModeAsync(
        LotteryMode mode,
        int modeIndex,
        int modeCount,
        ContestBulkUpdateRequest request,
        CancellationToken cancellationToken,
        ContestBulkUpdateProgressCallback? onProgress)
    {
        var latestContestNumber = await store.GetLatestContestNumberAsync(mode.Id, cancellationToken);
        var nextContestNumber = Math.Max(request.StartAt ?? 1, (latestContestNumber ?? 0) + 1);
        var importedContestNumbers = new List<int>();
        string status = "atualizado";
        string? error = null;

        if (onProgress is not null)
        {
            await onProgress(new ContestBulkUpdateProgress(
                Event: "modalidade_iniciada",
                ModeCode: mode.Code,
                ModeName: mode.Name,
                ModeIndex: modeIndex,
                ModeCount: modeCount,
                ContestNumber: null,
                MainNumbers: null,
                ImportedInMode: 0,
                ResumeFromContestNumber: nextContestNumber,
                LastSavedContestNumber: latestContestNumber,
                NextContestNumber: null,
                TotalInDatabase: null,
                Status: null,
                Error: null));
        }

        for (var attempt = 0; !request.MaxContestsPerMode.HasValue || attempt < request.MaxContestsPerMode.Value; attempt++)
        {
            try
            {
                var rawJson = await GetContestResultJsonWithRetryAsync(
                    mode,
                    modeIndex,
                    modeCount,
                    nextContestNumber,
                    request,
                    cancellationToken,
                    onProgress);
                var parsedResult = CaixaLotteryResultParser.Parse(rawJson);
                var contest = ContestImportMapper.Map(mode, parsedResult, rawJson);

                await store.SaveContestAsync(contest, cancellationToken);
                importedContestNumbers.Add(contest.Number);
                nextContestNumber = contest.Number + 1;

                if (onProgress is not null)
                {
                    var mainNumbers = contest.Numbers
                        .Where(number => number.NumberType == "principal")
                        .OrderBy(number => number.NumericValue)
                        .Select(number => number.Value)
                        .ToArray();
                    await onProgress(new ContestBulkUpdateProgress(
                        Event: "concurso_importado",
                        ModeCode: mode.Code,
                        ModeName: mode.Name,
                        ModeIndex: modeIndex,
                        ModeCount: modeCount,
                        ContestNumber: contest.Number,
                        MainNumbers: mainNumbers,
                        ImportedInMode: importedContestNumbers.Count,
                        ResumeFromContestNumber: null,
                        LastSavedContestNumber: null,
                        NextContestNumber: null,
                        TotalInDatabase: null,
                        Status: null,
                        Error: null));
                }

                if (request.DelayMilliseconds > 0)
                {
                    await Task.Delay(request.DelayMilliseconds, cancellationToken);
                }
            }
            catch (CaixaContestNotFoundException)
            {
                status = importedContestNumbers.Count == 0 ? "sem_novos_concursos" : "atualizado";
                break;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                status = "falhou";
                error = exception.Message;
                break;
            }
        }

        if (request.MaxContestsPerMode.HasValue && importedContestNumbers.Count == request.MaxContestsPerMode.Value)
        {
            status = "limite_atingido";
        }

        return new ContestBulkUpdateModeResult(
            ModeCode: mode.Code,
            ModeName: mode.Name,
            StartedAtContestNumber: nextContestNumber - importedContestNumbers.Count,
            NextContestNumber: nextContestNumber,
            ImportedContestNumbers: importedContestNumbers,
            Status: status,
            Error: error);
    }

    // Busca um concurso repetindo a mesma requisicao em erros temporarios da API da Caixa.
    private async Task<string> GetContestResultJsonWithRetryAsync(
        LotteryMode mode,
        int modeIndex,
        int modeCount,
        int contestNumber,
        ContestBulkUpdateRequest request,
        CancellationToken cancellationToken,
        ContestBulkUpdateProgressCallback? onProgress)
    {
        var transientFailures = 0;

        while (true)
        {
            try
            {
                return CaixaLotteryResultJsonSanitizer.Sanitize(
                    await client.GetContestResultJsonAsync(mode.Code, contestNumber, cancellationToken));
            }
            catch (CaixaTransientApiException) when (CanRetry(transientFailures, request.MaxRetryAttempts))
            {
                transientFailures++;

                // Avisa o progresso antes da espera para a tela nao parecer travada.
                if (onProgress is not null)
                {
                    await onProgress(new ContestBulkUpdateProgress(
                        Event: "tentativa_falhou",
                        ModeCode: mode.Code,
                        ModeName: mode.Name,
                        ModeIndex: modeIndex,
                        ModeCount: modeCount,
                        ContestNumber: contestNumber,
                        MainNumbers: null,
                        ImportedInMode: 0,
                        ResumeFromContestNumber: null,
                        LastSavedContestNumber: null,
                        NextContestNumber: null,
                        TotalInDatabase: null,
                        Status: null,
                        Error: null,
                        RetryAttempt: transientFailures,
                        RetryDelayMilliseconds: request.ErrorDelayMilliseconds));
                }

                if (request.ErrorDelayMilliseconds > 0)
                {
                    await Task.Delay(request.ErrorDelayMilliseconds, cancellationToken);
                }
            }
        }
    }

    // Define se ainda pode repetir uma chamada temporariamente rejeitada pela Caixa.
    private static bool CanRetry(int failures, int? maxRetryAttempts)
    {
        return !maxRetryAttempts.HasValue || failures < maxRetryAttempts.Value;
    }
}

public sealed record ContestBulkUpdateRequest(
    int? StartAt = null,
    int? MaxContestsPerMode = null,
    int DelayMilliseconds = 200,
    int ErrorDelayMilliseconds = 300000,
    int? MaxRetryAttempts = null);

public sealed record ContestBulkUpdateResult(
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    IReadOnlyList<ContestBulkUpdateModeResult> Modes)
{
    public int TotalImported => Modes.Sum(mode => mode.ImportedContestNumbers.Count);
}

public sealed record ContestBulkUpdateModeResult(
    string ModeCode,
    string ModeName,
    int StartedAtContestNumber,
    int NextContestNumber,
    IReadOnlyList<int> ImportedContestNumbers,
    string Status,
    string? Error);
