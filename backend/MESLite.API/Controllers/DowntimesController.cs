using MESLite.Application.Common.Dtos;
using MESLite.Application.Features.Downtimes;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class DowntimesController : ApiControllerBase
{
    /// <summary>Duruş analizi: sebep dağılımı, makine bazlı toplam, son duruşlar.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(DowntimeAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int days = 7, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetDowntimeAnalyticsQuery(days), ct));
}
