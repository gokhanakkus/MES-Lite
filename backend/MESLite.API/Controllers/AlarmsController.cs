using MESLite.Application.Common.Dtos;
using MESLite.Application.Features.Alarms;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class AlarmsController : ApiControllerBase
{
    /// <summary>Anomali/alarm listesi. activeOnly=true ile yalnızca aktif alarmlar.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AlarmDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] bool activeOnly = false,
        [FromQuery] int hours = 24,
        CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetAlarmsQuery(activeOnly, hours), ct));
}
