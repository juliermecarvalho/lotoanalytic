using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace LotoAnalytics.Api.Features.Contests;

[ApiController]
[Route("concursos")]
public sealed class ContestImportController(IContestImportService importService) : ControllerBase
{
    // Importa um concurso oficial da Caixa para uma modalidade cadastrada.
    [HttpPost("{codigoModalidade}/{numeroConcurso:int}/importar")]
    public async Task<ActionResult<ContestImportResponse>> Import(
        string codigoModalidade,
        int numeroConcurso,
        CancellationToken cancellationToken)
    {
        var result = await importService.ImportContestAsync(codigoModalidade, numeroConcurso, cancellationToken);

        return Ok(new ContestImportResponse(
            ModeCode: result.ModeCode,
            ContestNumber: result.ContestNumber,
            MainNumbersCount: result.MainNumbersCount,
            PrizeTiersCount: result.PrizeTiersCount));
    }
}

public sealed record ContestImportResponse(
    [property: JsonPropertyName("codigoModalidade")] string ModeCode,
    [property: JsonPropertyName("numeroConcurso")] int ContestNumber,
    [property: JsonPropertyName("quantidadeDezenasPrincipal")] int MainNumbersCount,
    [property: JsonPropertyName("quantidadeFaixasPremio")] int PrizeTiersCount);
