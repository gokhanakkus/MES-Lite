using MESLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core context so the Application layer stays persistence-agnostic.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Machine> Machines { get; }
    DbSet<Operator> Operators { get; }
    DbSet<ProductionRecord> ProductionRecords { get; }
    DbSet<Downtime> Downtimes { get; }
    DbSet<QualityRecord> QualityRecords { get; }
    DbSet<MachineTelemetrySnapshot> TelemetrySnapshots { get; }
    DbSet<Alarm> Alarms { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
