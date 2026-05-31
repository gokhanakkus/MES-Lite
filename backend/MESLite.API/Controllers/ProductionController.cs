using MESLite.Application.Common.Dtos;
using MESLite.Application.Features.Production;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class ProductionController : ApiControllerBase
{
    /// <summary>Canlı üretim akışı (en son üretim kayıtları). SignalR ile de canlı yayınlanır.</summary>
    [HttpGet("live")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductionRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLive([FromQuery] int take = 50, CancellationToken ct = default)
        => Ok(await Mediator.Send(new GetLiveProductionQuery(take), ct));
}
