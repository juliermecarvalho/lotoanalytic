using Microsoft.AspNetCore.Mvc;

namespace LotoAnalytics.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    // Retorna o estado basico da API para smoke tests e monitoramento inicial.
    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse("ok", "LotoAnalytics"));
    }
}

public sealed record HealthResponse(string Status, string Product);
