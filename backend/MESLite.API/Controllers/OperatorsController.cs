using MESLite.Application.Common.Dtos;
using MESLite.Application.Features.Operators;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class OperatorsController : ApiControllerBase
{
    /// <summary>Operatör performansı: toplam üretim, toplam duruş (dk), ortalama OEE.</summary>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(IReadOnlyList<OperatorPerformanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPerformance([FromQuery] int days = 7, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetOperatorPerformanceQuery(days), ct));
}
