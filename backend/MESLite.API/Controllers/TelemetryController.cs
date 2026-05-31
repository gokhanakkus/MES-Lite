using MESLite.Application.Common.Dtos;
using MESLite.Application.Features.Telemetry;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class TelemetryController : ApiControllerBase
{
    /// <summary>Son N dakikanın telemetri kayıtları (tüm makineler) — trend sparkline'ları için.</summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<TelemetryReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecent([FromQuery] int minutes = 30, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetRecentTelemetryQuery(minutes), ct));

    /// <summary>Tek makinenin son N dakikalık telemetri zaman serisi (detay grafikleri için).</summary>
    [HttpGet("{machineId:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<TelemetryReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForMachine(int machineId, [FromQuery] int minutes = 60, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetRecentTelemetryQuery(minutes, machineId), ct));
}
