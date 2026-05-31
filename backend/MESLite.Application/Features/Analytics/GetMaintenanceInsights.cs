using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;

namespace MESLite.Application.Features.Analytics;

/// <summary>GET /api/analytics/maintenance — AI maintenance insights over the last N days (default 30).</summary>
public sealed record GetMaintenanceInsightsQuery(int Days = 30) : IRequest<IReadOnlyList<MaintenanceInsightDto>>;

public sealed class GetMaintenanceInsightsQueryHandler
    : IRequestHandler<GetMaintenanceInsightsQuery, IReadOnlyList<MaintenanceInsightDto>>
{
    private readonly IAiAnalyticsService _ai;

    public GetMaintenanceInsightsQueryHandler(IAiAnalyticsService ai) => _ai = ai;

    public Task<IReadOnlyList<MaintenanceInsightDto>> Handle(GetMaintenanceInsightsQuery request, CancellationToken ct)
        => _ai.GetMaintenanceInsightsAsync(request.Days, ct);
}
