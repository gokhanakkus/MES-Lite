using MESLite.Application.Common.Dtos;
using MESLite.Application.Features.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class AnalyticsController : ApiControllerBase
{
    /// <summary>AI bakım önerileri: son N günün duruş verisine göre en problemli makineler ve öneriler.</summary>
    [HttpGet("maintenance")]
    [ProducesResponseType(typeof(IReadOnlyList<MaintenanceInsightDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaintenance([FromQuery] int days = 30, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetMaintenanceInsightsQuery(days), ct));

    /// <summary>Kalan ömür (RUL) tahmini: makine bazlı aşınma trendine göre kalan süre.</summary>
    [HttpGet("rul")]
    [ProducesResponseType(typeof(IReadOnlyList<RulDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRul([FromQuery] int windowMinutes = 30, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetRulQuery(windowMinutes), ct));
}
