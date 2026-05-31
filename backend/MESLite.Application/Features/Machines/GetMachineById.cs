using FluentValidation;
using MediatR;
using MESLite.Application.Common.Dtos;
using MESLite.Application.Common.Interfaces;
using MESLite.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Features.Machines;

/// <summary>GET /api/machines/{id} — single machine detail. Returns null when not found.</summary>
public sealed record GetMachineByIdQuery(int Id) : IRequest<MachineDto?>;

public sealed class GetMachineByIdQueryValidator : AbstractValidator<GetMachineByIdQuery>
{
    public GetMachineByIdQueryValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

public sealed class GetMachineByIdQueryHandler : IRequestHandler<GetMachineByIdQuery, MachineDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly IOeeCalculationService _oee;

    public GetMachineByIdQueryHandler(IApplicationDbContext db, IOeeCalculationService oee)
    {
        _db = db;
        _oee = oee;
    }

    public async Task<MachineDto?> Handle(GetMachineByIdQuery request, CancellationToken ct)
    {
        var m = await _db.Machines.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (m is null) return null;

        var (from, to) = PeriodType.Daily.ToRange(DateTime.UtcNow);
        var oee = await _oee.CalculateAsync(m.Id, from, to, ct);

        var hourAgo = DateTime.UtcNow.AddHours(-1);
        var lastHour = await _db.ProductionRecords.AsNoTracking()
            .Where(p => p.MachineId == m.Id && p.ProducedAt >= hourAgo)
            .SumAsync(p => (int?)p.Quantity, ct) ?? 0;

        var op = await _db.ProductionRecords.AsNoTracking()
            .Where(p => p.MachineId == m.Id && p.OperatorId != null)
            .OrderByDescending(p => p.ProducedAt)
            .Select(p => p.Operator!.FullName)
            .FirstOrDefaultAsync(ct);

        return new MachineDto(
            m.Id, m.Name, m.MachineType, m.MachineType.ToString(),
            m.Status, m.Status.ToString(), m.ProductionLine, m.IdealRunRatePerHour,
            op, lastHour, Math.Round(oee.Oee * 100, 1),
            Math.Round(m.HealthScore, 1), Math.Round(m.WearLevel, 1), m.Rpm,
            Math.Round(m.Speed, 1), Math.Round(m.Load, 1), Math.Round(m.Vibration, 2),
            Math.Round(m.Temperature, 1), Math.Round(m.Efficiency * 100, 1), Math.Round(m.CycleTimeSeconds, 2));
    }
}
