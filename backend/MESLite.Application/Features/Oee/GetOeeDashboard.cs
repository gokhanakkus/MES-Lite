using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using MESLite.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Oee;

/// <summary>GET /api/oee/dashboard — per-machine OEE breakdown + plant averages for a period (daily/weekly/monthly).</summary>
public sealed record GetOeeDashboardQuery(PeriodType Period = PeriodType.Daily) : IRequest<OeeDashboardDto>;

public sealed class GetOeeDashboardQueryHandler : IRequestHandler<GetOeeDashboardQuery, OeeDashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly IOeeCalculationService _oee;

    public GetOeeDashboardQueryHandler(IApplicationDbContext db, IOeeCalculationService oee)
    {
        _db = db;
        _oee = oee;
    }

    public async Task<OeeDashboardDto> Handle(GetOeeDashboardQuery request, CancellationToken ct)
    {
        var (from, to) = request.Period.ToRange(DateTime.UtcNow);

        var machines = await _db.Machines.AsNoTracking()
            .Select(m => new { m.Id, m.Name })
            .OrderBy(m => m.Name)
            .ToListAsync(ct);

        var oee = await _oee.CalculateForAllAsync(from, to, ct);

        var items = machines.Select(m =>
        {
            var r = oee.GetValueOrDefault(m.Id);
            return new OeeDto(
                m.Id, m.Name,
                Round(r?.Availability), Round(r?.Performance), Round(r?.Quality), Round(r?.Oee));
        }).ToList();

        return new OeeDashboardDto(
            items.Count == 0 ? 0 : Math.Round(items.Average(i => i.Oee), 1),
            items.Count == 0 ? 0 : Math.Round(items.Average(i => i.Availability), 1),
            items.Count == 0 ? 0 : Math.Round(items.Average(i => i.Performance), 1),
            items.Count == 0 ? 0 : Math.Round(items.Average(i => i.Quality), 1),
            items);
    }

    private static double Round(double? ratio) => Math.Round((ratio ?? 0) * 100, 1);
}
