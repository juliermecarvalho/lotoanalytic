using Microsoft.Extensions.Options;

namespace LotoAnalytics.Api.Features.Contests;

public sealed class ContestUpdateHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<ContestUpdateScheduleOptions> options,
    TimeProvider timeProvider,
    ILogger<ContestUpdateHostedService> logger) : BackgroundService
{
    // Executa a atualizacao inicial sem bloquear a inicializacao e mantem o agendamento diario.
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var currentOptions = options.Value;
        if (!currentOptions.Enabled)
        {
            logger.LogInformation("Atualizacao automatica de concursos desabilitada.");
            return;
        }

        if (currentOptions.RunOnStartup)
        {
            _ = RunUpdateSafelyAsync("startup", stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ContestUpdateScheduleCalculator.CalculateDelayUntilNextRun(
                timeProvider.GetUtcNow(),
                currentOptions.DailyRunAt,
                currentOptions.TimeZoneId);

            logger.LogInformation(
                "Proxima atualizacao automatica de concursos em {Delay}.",
                delay);

            await Task.Delay(delay, timeProvider, stoppingToken);
            await RunUpdateSafelyAsync("scheduled", stoppingToken);
        }
    }

    // Executa a sincronizacao em um escopo isolado e registra falhas sem encerrar o host.
    private async Task RunUpdateSafelyAsync(string trigger, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var updateService = scope.ServiceProvider.GetRequiredService<IContestBulkUpdateService>();
            var currentOptions = options.Value;
            var result = await updateService.UpdateAllAsync(
                new ContestBulkUpdateRequest(
                    DelayMilliseconds: currentOptions.DelayMilliseconds,
                    ErrorDelayMilliseconds: currentOptions.ErrorDelayMilliseconds,
                    MaxContestsPerMode: currentOptions.MaxContestsPerMode,
                    MaxRetryAttempts: currentOptions.MaxRetryAttempts),
                cancellationToken);

            logger.LogInformation(
                "Atualizacao automatica de concursos concluida por {Trigger}. Total importado: {TotalImported}.",
                trigger,
                result.TotalImported);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Atualizacao automatica de concursos cancelada.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha na atualizacao automatica de concursos por {Trigger}.", trigger);
        }
    }
}

public sealed record ContestUpdateScheduleOptions
{
    public const string SectionName = "ContestUpdates";

    public bool Enabled { get; init; } = true;

    public bool RunOnStartup { get; init; } = true;

    public TimeOnly DailyRunAt { get; init; } = new(1, 0);

    public string TimeZoneId { get; init; } = "E. South America Standard Time";

    public int DelayMilliseconds { get; init; } = 200;

    public int ErrorDelayMilliseconds { get; init; } = 300000;

    public int? MaxRetryAttempts { get; init; }

    public int? MaxContestsPerMode { get; init; }
}

public static class ContestUpdateScheduleCalculator
{
    // Calcula o intervalo ate a proxima execucao diaria no fuso configurado.
    public static TimeSpan CalculateDelayUntilNextRun(
        DateTimeOffset utcNow,
        TimeOnly dailyRunAt,
        string timeZoneId)
    {
        var timeZone = FindTimeZone(timeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var nextLocalRun = localNow.Date + dailyRunAt.ToTimeSpan();

        if (nextLocalRun <= localNow.DateTime)
        {
            nextLocalRun = nextLocalRun.AddDays(1);
        }

        var nextRun = new DateTimeOffset(nextLocalRun, timeZone.GetUtcOffset(nextLocalRun));
        var nextRunUtc = TimeZoneInfo.ConvertTime(nextRun, TimeZoneInfo.Utc);

        return nextRunUtc - utcNow;
    }

    // Localiza o fuso horario aceitando Windows ID e fallback Linux para Sao Paulo.
    private static TimeZoneInfo FindTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException) when (timeZoneId == "E. South America Standard Time")
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (InvalidTimeZoneException) when (timeZoneId == "E. South America Standard Time")
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
    }
}
