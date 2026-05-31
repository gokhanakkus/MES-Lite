using MESLite.Application.Common.Dtos;
using MESLite.Application.Features.Machines;
using Microsoft.AspNetCore.Mvc;

namespace MESLite.API.Controllers;

public sealed class MachinesController : ApiControllerBase
{
    /// <summary>Tüm makineler (durum, operatör, anlık üretim, OEE ile).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MachineDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await Mediator.Send(new GetMachinesQuery(), ct));

    /// <summary>Tek makine detayı.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MachineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var machine = await Mediator.Send(new GetMachineByIdQuery(id), ct);
        return machine is null ? NotFound() : Ok(machine);
    }
}
