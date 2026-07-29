using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace LotoAnalytics.Api.Features.Statistics;

[ApiController]
[Route("estatisticas/lotofacil")]
public sealed class LotofacilStatisticsController : ControllerBase
{
    // Calcula um resumo estatistico da Lotofacil para um conjunto informado de dezenas.
    [HttpPost("calcular")]
    public ActionResult<LotofacilStatisticsResponse> Calculate([FromBody] CalculateLotofacilStatisticsRequest request)
    {
        var statistics = LotofacilStatisticsCalculator.Calculate(request.Numbers, request.PreviousNumbers);

        return Ok(new LotofacilStatisticsResponse(
            EvenCount: statistics.EvenCount,
            OddCount: statistics.OddCount,
            NumbersSum: statistics.NumbersSum,
            RepeatedFromPrevious: statistics.RepeatedFromPrevious,
            PrimeCount: statistics.PrimeCount,
            BorderCount: statistics.BorderCount,
            CenterCount: statistics.CenterCount,
            LongestSequence: statistics.LongestSequence,
            RowDistribution: statistics.RowDistribution,
            ColumnDistribution: statistics.ColumnDistribution));
    }
}

public sealed record CalculateLotofacilStatisticsRequest
{
    [JsonPropertyName("dezenas")]
    [Required]
    [MinLength(15)]
    [MaxLength(16)]
    public required IReadOnlyList<string> Numbers { get; init; }

    [JsonPropertyName("dezenasAnteriores")]
    public IReadOnlyList<string>? PreviousNumbers { get; init; }
}

public sealed record LotofacilStatisticsResponse(
    [property: JsonPropertyName("quantidadePares")] int EvenCount,
    [property: JsonPropertyName("quantidadeImpares")] int OddCount,
    [property: JsonPropertyName("somaDezenas")] int NumbersSum,
    [property: JsonPropertyName("repetidasAnterior")] IReadOnlyList<string> RepeatedFromPrevious,
    [property: JsonPropertyName("quantidadePrimos")] int PrimeCount,
    [property: JsonPropertyName("quantidadeMoldura")] int BorderCount,
    [property: JsonPropertyName("quantidadeMiolo")] int CenterCount,
    [property: JsonPropertyName("maiorSequencia")] int LongestSequence,
    [property: JsonPropertyName("distribuicaoLinhas")] IReadOnlyList<int> RowDistribution,
    [property: JsonPropertyName("distribuicaoColunas")] IReadOnlyList<int> ColumnDistribution);
