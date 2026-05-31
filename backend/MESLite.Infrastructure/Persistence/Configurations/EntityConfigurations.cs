using MESLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MESLite.Infrastructure.Persistence.Configurations;

public sealed class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.ProductionLine).HasMaxLength(100).IsRequired();
        b.Property(x => x.MachineType).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Shift).HasConversion<int>();
    }
}

public sealed class ProductionRecordConfiguration : IEntityTypeConfiguration<ProductionRecord>
{
    public void Configure(EntityTypeBuilder<ProductionRecord> b)
    {
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Machine).WithMany(m => m.ProductionRecords)
            .HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Operator).WithMany(o => o.ProductionRecords)
            .HasForeignKey(x => x.OperatorId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.ProducedAt);
        b.HasIndex(x => new { x.MachineId, x.ProducedAt });
    }
}

public sealed class DowntimeConfiguration : IEntityTypeConfiguration<Downtime>
{
    public void Configure(EntityTypeBuilder<Downtime> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasConversion<int>();
        b.HasOne(x => x.Machine).WithMany(m => m.Downtimes)
            .HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.StartTime);
        b.HasIndex(x => new { x.MachineId, x.StartTime });
        b.Ignore(x => x.IsOngoing);
        b.Ignore(x => x.Duration);
    }
}

public sealed class MachineTelemetrySnapshotConfiguration : IEntityTypeConfiguration<MachineTelemetrySnapshot>
{
    public void Configure(EntityTypeBuilder<MachineTelemetrySnapshot> b)
    {
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Machine).WithMany()
            .HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.MachineId, x.CreatedAt });
        b.HasIndex(x => x.CreatedAt);
    }
}

public sealed class AlarmConfiguration : IEntityTypeConfiguration<Alarm>
{
    public void Configure(EntityTypeBuilder<Alarm> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Metric).HasConversion<int>();
        b.Property(x => x.Severity).HasConversion<int>();
        b.Property(x => x.Detector).HasConversion<int>();
        b.Property(x => x.Message).HasMaxLength(300);
        b.HasOne(x => x.Machine).WithMany()
            .HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.RaisedAt);
        b.HasIndex(x => new { x.MachineId, x.ResolvedAt });
        b.Ignore(x => x.IsActive);
    }
}

public sealed class QualityRecordConfiguration : IEntityTypeConfiguration<QualityRecord>
{
    public void Configure(EntityTypeBuilder<QualityRecord> b)
    {
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Machine).WithMany(m => m.QualityRecords)
            .HasForeignKey(x => x.MachineId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.CreatedAt);
        b.Ignore(x => x.QualityRate);
    }
}
