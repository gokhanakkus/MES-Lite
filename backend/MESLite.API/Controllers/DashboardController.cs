using MESLite.Application.Common.Dtos;
using MESLite.Application.Features.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class DashboardController : ApiControllerBase
{
    /// <summary>Üst KPI kartları: toplam/çalışan/duran/bakım makine, bugünkü üretim, ortalama OEE.</summary>
    [HttpGet("kpis")]
    [ProducesResponseType(typeof(DashboardKpiDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKpis(CancellationToken ct)
        => Ok(await Mediator.Send(new GetDashboardKpisQuery(), ct));
}
