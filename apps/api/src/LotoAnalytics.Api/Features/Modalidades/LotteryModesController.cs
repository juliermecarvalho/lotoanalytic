using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Features.Modalidades;

[ApiController]
[Route("modalidades")]
public sealed class LotteryModesController(LotoAnalyticsDbContext dbContext) : ControllerBase
{
    // Lista as modalidades ativas cadastradas no banco em ordem de seed.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LotteryModeResponse>>> Get(CancellationToken cancellationToken)
    {
        var modalidades = await dbContext.LotteryModes
            .AsNoTracking()
            .Where(mode => mode.Active)
            .OrderBy(mode => mode.Id)
            .Select(mode => new LotteryModeResponse(
                mode.Code,
                mode.Name,
                mode.CaixaGameType,
                mode.MainNumbersCount,
                mode.SimpleBetPrice,
                mode.Active))
            .ToListAsync(cancellationToken);

        return Ok(modalidades);
    }
}

public sealed record LotteryModeResponse(
    string Codigo,
    string Nome,
    string TipoJogoCaixa,
    int QuantidadeDezenasPrincipal,
    decimal? ValorApostaSimples,
    bool Ativa);
