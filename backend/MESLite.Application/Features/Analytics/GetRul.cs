using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;

namespace MESLite.Application.Features.Analytics;

/// <summary>GET /api/analytics/rul — Remaining Useful Life per machine from the recent wear trend.</summary>
public sealed record GetRulQuery(int WindowMinutes = 30) : IRequest<IReadOnlyList<RulDto>>;

public sealed class GetRulQueryHandler : IRequestHandler<GetRulQuery, IReadOnlyList<RulDto>>
{
    private readonly IPredictiveMaintenanceService _service;

    public GetRulQueryHandler(IPredictiveMaintenanceService service) => _service = service;

    public Task<IReadOnlyList<RulDto>> Handle(GetRulQuery request, CancellationToken ct)
        => _service.GetRemainingUsefulLifeAsync(request.WindowMinutes, ct);
}
