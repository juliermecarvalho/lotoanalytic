using System.Text.Json.Serialization;
using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Features.Contests;

[ApiController]
[Route("concursos")]
public sealed class LatestContestController(LotoAnalyticsDbContext dbContext) : ControllerBase
{
    // Consulta o concurso mais recente registrado no banco para uma modalidade.
    [HttpGet("{codigoModalidade}/ultimo")]
    public async Task<ActionResult<LatestContestResponse>> GetLatest(
        string codigoModalidade,
        CancellationToken cancellationToken)
    {
        var contest = await dbContext.Contests
            .AsNoTracking()
            .Where(candidate => candidate.LotteryMode!.Code == codigoModalidade)
            .OrderByDescending(candidate => candidate.Number)
            .Select(candidate => new
            {
                candidate.Number,
                candidate.DrawDate,
                MainNumbers = candidate.Numbers
                    .Where(number => number.NumberType == "principal")
                    .OrderBy(number => number.NumericValue)
                    .Select(number => number.Value)
                    .ToArray()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (contest is null)
        {
            return NotFound(new { erro = "Nenhum concurso registrado para a modalidade." });
        }

        var totalContests = await dbContext.Contests
            .AsNoTracking()
            .CountAsync(candidate => candidate.LotteryMode!.Code == codigoModalidade, cancellationToken);

        return Ok(new LatestContestResponse(
            ModeCode: codigoModalidade,
            ContestNumber: contest.Number,
            DrawDate: contest.DrawDate,
            MainNumbers: contest.MainNumbers,
            TotalContests: totalContests));
    }
}

public sealed record LatestContestResponse(
    [property: JsonPropertyName("codigoModalidade")] string ModeCode,
    [property: JsonPropertyName("numeroConcurso")] int ContestNumber,
    [property: JsonPropertyName("dataApuracao")] DateOnly? DrawDate,
    [property: JsonPropertyName("dezenas")] IReadOnlyList<string> MainNumbers,
    [property: JsonPropertyName("totalConcursos")] int TotalContests);
