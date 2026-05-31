using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Production;

/// <summary>GET /api/production/live — most recent production ticks across all machines.</summary>
public sealed record GetLiveProductionQuery(int Take = 50) : IRequest<IReadOnlyList<ProductionRecordDto>>;

public sealed class GetLiveProductionQueryHandler : IRequestHandler<GetLiveProductionQuery, IReadOnlyList<ProductionRecordDto>>
{
    private readonly IApplicationDbContext _db;

    public GetLiveProductionQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductionRecordDto>> Handle(GetLiveProductionQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.Take, 1, 200);

        return await _db.ProductionRecords.AsNoTracking()
            .OrderByDescending(p => p.ProducedAt)
            .Take(take)
            .Select(p => new ProductionRecordDto(
                p.Id,
                p.MachineId,
                p.Machine.Name,
                p.OperatorId,
                p.Operator != null ? p.Operator.FullName : null,
                p.Quantity,
                p.ProducedAt))
            .ToListAsync(ct);
    }
}
