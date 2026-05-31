using MESLite.Application.Common.Dtos;
using MESLite.Application.Features.Quality;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class QualityController : ApiControllerBase
{
    /// <summary>Kalite özeti: toplam üretim, hatalı üretim, kalite yüzdesi, makine bazlı hata dağılımı.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(QualitySummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int days = 7, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetQualitySummaryQuery(days), ct));
}
