using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotoAnalytics.Api.Features.Contests;

[ApiController]
[Authorize(Roles = "administrador")]
[Route("admin/concursos")]
public sealed class AdminContestUpdateController(IContestBulkUpdateService updateService) : ControllerBase
{
    private static readonly JsonSerializerOptions StreamJsonOptions = new(JsonSerializerDefaults.Web);

    // Atualiza todas as modalidades ativas buscando os proximos concursos na Caixa.
    [HttpPost("atualizar-todos")]
    public async Task<ActionResult<ContestBulkUpdateResponse>> UpdateAll(
        [FromBody] ContestBulkUpdateHttpRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await updateService.UpdateAllAsync(MapRequest(request), cancellationToken);

        return Ok(MapResponse(result));
    }

    // Atualiza todas as modalidades emitindo uma linha NDJSON por concurso importado.
    [HttpPost("atualizar-todos/progresso")]
    public async Task UpdateAllWithProgress(
        [FromBody] ContestBulkUpdateHttpRequest? request,
        CancellationToken cancellationToken)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "application/x-ndjson; charset=utf-8";

        var result = await updateService.UpdateAllAsync(
            MapRequest(request),
            cancellationToken,
            progress => WriteStreamEventAsync(
                new ContestBulkUpdateProgressResponse(
                    Evento: progress.Event,
                    CodigoModalidade: progress.ModeCode,
                    NomeModalidade: progress.ModeName,
                    IndiceModalidade: progress.ModeIndex,
                    TotalModalidades: progress.ModeCount,
                    NumeroConcurso: progress.ContestNumber,
                    Dezenas: progress.MainNumbers,
                    QuantidadeImportada: progress.ImportedInMode,
                    RetomarDoConcurso: progress.ResumeFromContestNumber,
                    UltimoConcursoSalvo: progress.LastSavedContestNumber,
                    ProximoConcurso: progress.NextContestNumber,
                    TotalNoBanco: progress.TotalInDatabase,
                    Status: progress.Status,
                    Erro: progress.Error,
                    Tentativa: progress.RetryAttempt,
                    AguardarMs: progress.RetryDelayMilliseconds),
                cancellationToken));

        await WriteStreamEventAsync(
            new ContestBulkUpdateCompletedResponse(Evento: "concluido", Resultado: MapResponse(result)),
            cancellationToken);
    }

    // Serializa um evento como linha NDJSON e envia imediatamente para o cliente.
    private async Task WriteStreamEventAsync<TEvent>(TEvent payload, CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(payload, StreamJsonOptions) + "\n";
        await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(line), cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }

    // Converte o contrato HTTP em PT-BR para a requisicao interna do servico.
    private static ContestBulkUpdateRequest MapRequest(ContestBulkUpdateHttpRequest? request)
    {
        return new ContestBulkUpdateRequest(
            StartAt: request?.Inicio,
            MaxContestsPerMode: request?.LimitePorModalidade,
            DelayMilliseconds: request?.PausaMs ?? 200,
            ErrorDelayMilliseconds: request?.PausaErroMs ?? 300000,
            MaxRetryAttempts: request?.MaxTentativasErro);
    }

    // Converte o resultado interno para o contrato HTTP em PT-BR.
    private static ContestBulkUpdateResponse MapResponse(ContestBulkUpdateResult result)
    {
        return new ContestBulkUpdateResponse(
            InicioEm: result.StartedAt,
            FinalizadoEm: result.FinishedAt,
            TotalImportado: result.TotalImported,
            Modalidades: result.Modes.Select(mode => new ContestBulkUpdateModeResponse(
                CodigoModalidade: mode.ModeCode,
                NomeModalidade: mode.ModeName,
                ConcursoInicial: mode.StartedAtContestNumber,
                ProximoConcurso: mode.NextContestNumber,
                ConcursosImportados: mode.ImportedContestNumbers,
                QuantidadeImportada: mode.ImportedContestNumbers.Count,
                Status: mode.Status,
                Erro: mode.Error)).ToArray());
    }
}

public sealed record ContestBulkUpdateProgressResponse(
    [property: JsonPropertyName("evento")] string Evento,
    [property: JsonPropertyName("codigoModalidade")] string CodigoModalidade,
    [property: JsonPropertyName("nomeModalidade")] string NomeModalidade,
    [property: JsonPropertyName("indiceModalidade")] int IndiceModalidade,
    [property: JsonPropertyName("totalModalidades")] int TotalModalidades,
    [property: JsonPropertyName("numeroConcurso")] int? NumeroConcurso,
    [property: JsonPropertyName("dezenas")] IReadOnlyList<string>? Dezenas,
    [property: JsonPropertyName("quantidadeImportada")] int QuantidadeImportada,
    [property: JsonPropertyName("retomarDoConcurso")] int? RetomarDoConcurso,
    [property: JsonPropertyName("ultimoConcursoSalvo")] int? UltimoConcursoSalvo,
    [property: JsonPropertyName("proximoConcurso")] int? ProximoConcurso,
    [property: JsonPropertyName("totalNoBanco")] int? TotalNoBanco,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("erro")] string? Erro,
    [property: JsonPropertyName("tentativa")] int? Tentativa,
    [property: JsonPropertyName("aguardarMs")] int? AguardarMs);

public sealed record ContestBulkUpdateCompletedResponse(
    [property: JsonPropertyName("evento")] string Evento,
    [property: JsonPropertyName("resultado")] ContestBulkUpdateResponse Resultado);

public sealed record ContestBulkUpdateHttpRequest(
    [property: JsonPropertyName("inicio")]
    [Range(1, int.MaxValue)]
    int? Inicio,

    [property: JsonPropertyName("limitePorModalidade")]
    [Range(1, 1000)]
    int? LimitePorModalidade,

    [property: JsonPropertyName("pausaMs")]
    [Range(0, 10000)]
    int? PausaMs,

    [property: JsonPropertyName("pausaErroMs")]
    [Range(0, 3600000)]
    int? PausaErroMs,

    [property: JsonPropertyName("maxTentativasErro")]
    [Range(1, 100)]
    int? MaxTentativasErro);

public sealed record ContestBulkUpdateResponse(
    [property: JsonPropertyName("inicioEm")] DateTimeOffset InicioEm,
    [property: JsonPropertyName("finalizadoEm")] DateTimeOffset FinalizadoEm,
    [property: JsonPropertyName("totalImportado")] int TotalImportado,
    [property: JsonPropertyName("modalidades")] IReadOnlyList<ContestBulkUpdateModeResponse> Modalidades);

public sealed record ContestBulkUpdateModeResponse(
    [property: JsonPropertyName("codigoModalidade")] string CodigoModalidade,
    [property: JsonPropertyName("nomeModalidade")] string NomeModalidade,
    [property: JsonPropertyName("concursoInicial")] int ConcursoInicial,
    [property: JsonPropertyName("proximoConcurso")] int ProximoConcurso,
    [property: JsonPropertyName("concursosImportados")] IReadOnlyList<int> ConcursosImportados,
    [property: JsonPropertyName("quantidadeImportada")] int QuantidadeImportada,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("erro")] string? Erro);
