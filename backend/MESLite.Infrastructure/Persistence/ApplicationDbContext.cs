using MESLite.Application.Common.Interfaces;
using MESLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MESLite.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Machine> Machines => Set<Machine>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();
    public DbSet<Downtime> Downtimes => Set<Downtime>();
    public DbSet<QualityRecord> QualityRecords => Set<QualityRecord>();
    public DbSet<MachineTelemetrySnapshot> TelemetrySnapshots => Set<MachineTelemetrySnapshot>();
    public DbSet<Alarm> Alarms => Set<Alarm>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
