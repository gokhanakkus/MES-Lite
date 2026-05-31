using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Models;
using MESLite.Application.Features.Oee;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

[Route("api/oee")]
public sealed class OeeController : ApiControllerBase
{
    /// <summary>OEE dashboard. period = Daily | Weekly | Monthly.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(OeeDashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard([FromQuery] PeriodType period = PeriodType.Daily, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetOeeDashboardQuery(period), ct));
}
