using MESLite.Domain.Entities;
using MESLite.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MESLite.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds the database: 9 machines, 6 operators, and 30 days of synthetic
/// production / downtime / quality history so OEE, reports and AI insights have data on first run.
/// </summary>
public static class DbInitializer
{
    private static readonly Random Rng = new(20260530);

    public static async Task InitializeAsync(ApplicationDbContext db, ILogger logger, bool seedHistory = true)
    {
        // SQL Server uses real migrations; SQLite (the dependency-free fallback) just creates the schema.
        if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            await db.Database.MigrateAsync();
        }

        // Resolve any alarms left active from a previous run so the simulator starts from a clean slate.
        await db.Alarms
            .Where(a => a.ResolvedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ResolvedAt, DateTime.UtcNow));

        if (!await db.Machines.AnyAsync())
        {
            logger.LogInformation("Seeding machines and operators...");
            await SeedMachinesAndOperatorsAsync(db);
        }

        if (seedHistory && !await db.ProductionRecords.AnyAsync())
        {
            logger.LogInformation("Seeding 30 days of historical production data...");
            await SeedHistoryAsync(db);
        }

        if (seedHistory && !await db.TelemetrySnapshots.AnyAsync())
        {
            logger.LogInformation("Seeding telemetry time-series history...");
            await SeedTelemetryHistoryAsync(db);
        }

        if (seedHistory && !await db.Alarms.AnyAsync())
        {
            logger.LogInformation("Seeding alarm history...");
            await SeedAlarmHistoryAsync(db);
        }
    }

    /// <summary>A handful of resolved alarms over the last few hours so the Alarms page has history on first run.</summary>
    private static async Task SeedAlarmHistoryAsync(ApplicationDbContext db)
    {
        var machines = await db.Machines.ToListAsync();
        Machine? Find(string name) => machines.FirstOrDefault(m => m.Name == name);
        var now = DateTime.UtcNow;

        var seeds = new[]
        {
            (Name: "Dokuma-03", Metric: AlarmMetric.Vibration, Sev: AlarmSeverity.Warning, Det: AlarmDetector.Threshold, Msg: "Titreşim uyarı eşiğini aştı: 5.4 mm/s (limit 5.0 mm/s)", Val: 5.4, Lim: 5.0, HoursAgo: 3.5, DurMin: 22),
            (Name: "Dokuma-03", Metric: AlarmMetric.Health, Sev: AlarmSeverity.Warning, Det: AlarmDetector.Threshold, Msg: "Sağlık skoru düşük: 38 (limit 40)", Val: 38, Lim: 40, HoursAgo: 3.2, DurMin: 35),
            (Name: "Boya-02", Metric: AlarmMetric.Temperature, Sev: AlarmSeverity.Critical, Det: AlarmDetector.Threshold, Msg: "Sıcaklık kritik eşiği aştı: 85.1°C (limit 84°C)", Val: 85.1, Lim: 84, HoursAgo: 2.6, DurMin: 12),
            (Name: "Kesim-01", Metric: AlarmMetric.Rpm, Sev: AlarmSeverity.Warning, Det: AlarmDetector.Statistical, Msg: "Devir geçmiş ortalamanın üzerinde: 790 RPM (ort 705 ± 26, z=3.3)", Val: 790, Lim: 783, HoursAgo: 1.8, DurMin: 8),
            (Name: "Dokuma-01", Metric: AlarmMetric.Wear, Sev: AlarmSeverity.Warning, Det: AlarmDetector.Threshold, Msg: "Aşınma uyarı eşiğini aştı: 72% (limit 70%)", Val: 72, Lim: 70, HoursAgo: 1.1, DurMin: 40),
            (Name: "Boya-01", Metric: AlarmMetric.Temperature, Sev: AlarmSeverity.Warning, Det: AlarmDetector.Statistical, Msg: "Sıcaklık geçmiş ortalamanın üzerinde: 74.2°C (ort 63 ± 3.4, z=3.3)", Val: 74.2, Lim: 73.2, HoursAgo: 0.6, DurMin: 9),
        };

        var alarms = new List<Alarm>();
        foreach (var s in seeds)
        {
            var m = Find(s.Name);
            if (m is null) continue;
            var raised = now.AddHours(-s.HoursAgo);
            alarms.Add(new Alarm
            {
                MachineId = m.Id,
                Metric = s.Metric,
                Severity = s.Sev,
                Detector = s.Det,
                Message = s.Msg,
                Value = s.Val,
                Limit = s.Lim,
                RaisedAt = raised,
                ResolvedAt = raised.AddMinutes(s.DurMin)
            });
        }

        db.Alarms.AddRange(alarms);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seed ~40 minutes of telemetry per machine (every 30s) with a gently rising wear trend, so the
    /// sparklines are populated and the RUL regression has a meaningful slope on first run.
    /// </summary>
    private static async Task SeedTelemetryHistoryAsync(ApplicationDbContext db)
    {
        var machines = await db.Machines.ToListAsync();
        var now = DateTime.UtcNow;
        var snapshots = new List<MachineTelemetrySnapshot>();

        foreach (var m in machines)
        {
            const int points = 80;          // 80 × 30s ≈ 40 min
            const int stepSeconds = 30;
            var endWear = m.WearLevel <= 0 ? 20 : m.WearLevel;
            var startWear = Math.Max(0, endWear - 10); // rising trend toward current wear

            for (var i = points; i >= 0; i--)
            {
                var at = now.AddSeconds(-i * stepSeconds);
                var t = 1.0 - (double)i / points; // 0..1 over the window
                var wear = Math.Clamp(startWear + (endWear - startWear) * t + (Rng.NextDouble() - 0.5), 0, 100);
                var load = 45 + Rng.Next(0, 40);
                var vibration = Math.Round(1.2 + wear * 0.045 + (load - 50) * 0.02, 2);
                var temperature = Math.Round(40 + load * 0.30 + wear * 0.12 + (Rng.NextDouble() * 2 - 1), 1);
                var health = Math.Round(Math.Clamp(100 - wear * 0.7 - Math.Max(0, vibration - 4) * 4 - Math.Max(0, temperature - 75) * 1.2, 0, 100), 1);
                var efficiency = Math.Round(0.82 * (0.55 + 0.45 * (health / 100.0)), 3);

                snapshots.Add(new MachineTelemetrySnapshot
                {
                    MachineId = m.Id,
                    CreatedAt = at,
                    WearLevel = Math.Round(wear, 1),
                    Load = load,
                    Rpm = m.Rpm,
                    Speed = Math.Round(m.IdealRunRatePerHour * efficiency, 1),
                    Vibration = vibration,
                    Temperature = temperature,
                    HealthScore = health,
                    Efficiency = efficiency
                });
            }
        }

        db.TelemetrySnapshots.AddRange(snapshots);
        await db.SaveChangesAsync();
    }

    private static async Task SeedMachinesAndOperatorsAsync(ApplicationDbContext db)
    {
        var machines = new List<Machine>();
        for (var i = 1; i <= 3; i++)
        {
            machines.Add(new Machine { Name = $"Dokuma-{i:00}", MachineType = MachineType.Weaving, ProductionLine = "Hat-A", IdealRunRatePerHour = 500, Status = MachineStatus.Running });
            machines.Add(new Machine { Name = $"Boya-{i:00}", MachineType = MachineType.Dyeing, ProductionLine = "Hat-B", IdealRunRatePerHour = 1000, Status = MachineStatus.Running });
            machines.Add(new Machine { Name = $"Kesim-{i:00}", MachineType = MachineType.Cutting, ProductionLine = "Hat-C", IdealRunRatePerHour = 700, Status = MachineStatus.Running });
        }

        var operators = new List<Operator>
        {
            new() { FullName = "Ahmet Yılmaz", Shift = Shift.Morning },
            new() { FullName = "Mehmet Demir", Shift = Shift.Morning },
            new() { FullName = "Ayşe Kaya", Shift = Shift.Evening },
            new() { FullName = "Fatma Şahin", Shift = Shift.Evening },
            new() { FullName = "Mustafa Çelik", Shift = Shift.Night },
            new() { FullName = "Zeynep Arslan", Shift = Shift.Night },
        };

        foreach (var m in machines)
        {
            SeedTelemetry(m);
        }

        db.Machines.AddRange(machines);
        db.Operators.AddRange(operators);
        await db.SaveChangesAsync();
    }

    /// <summary>Give each machine plausible initial telemetry so the REST snapshot is populated before
    /// the simulator's first tick. Dokuma-03 starts worn (the "problem child").</summary>
    private static void SeedTelemetry(Machine m)
    {
        var (rpmMin, rpmMax, baseVib) = m.MachineType switch
        {
            MachineType.Weaving => (600, 1200, 1.5),
            MachineType.Dyeing => (100, 300, 1.0),
            _ => (300, 800, 1.2)
        };

        var isProblem = m is { MachineType: MachineType.Weaving } && m.Name.EndsWith("03");
        m.WearLevel = isProblem ? Rng.Next(72, 84) : Rng.Next(5, 38);
        m.Load = Rng.Next(45, 85);
        m.Rpm = (int)(rpmMin + (rpmMax - rpmMin) * (m.Load / 100.0));
        m.Vibration = Math.Round(baseVib + m.WearLevel * 0.045 + (m.Load - 50) * 0.02, 2);
        m.Temperature = Math.Round(40 + m.Load * 0.30 + m.WearLevel * 0.12, 1);
        m.HealthScore = Math.Round(Math.Clamp(100 - m.WearLevel * 0.7 - Math.Max(0, m.Vibration - 4) * 4 - Math.Max(0, m.Temperature - 75) * 1.2, 0, 100), 1);
        m.Efficiency = Math.Round((0.82) * (0.55 + 0.45 * (m.HealthScore / 100.0)), 3);
        m.Speed = Math.Round(m.IdealRunRatePerHour * m.Efficiency, 1);
        m.CycleTimeSeconds = m.Speed > 0 ? Math.Round(3600.0 / m.Speed, 2) : 0;
    }

    private static async Task SeedHistoryAsync(ApplicationDbContext db)
    {
        var machines = await db.Machines.ToListAsync();
        var operators = await db.Operators.ToListAsync();

        var production = new List<ProductionRecord>();
        var downtimes = new List<Downtime>();
        var quality = new List<QualityRecord>();

        var now = DateTime.UtcNow;
        var startDay = now.Date.AddDays(-30);

        foreach (var m in machines)
        {
            var profile = GetProfile(m);
            // Iterate 31 days INCLUDING today's partial day so the rolling 24h window is populated.
            for (var day = 0; day <= 30; day++)
            {
                var dayStart = startDay.AddDays(day);
                if (dayStart > now) break;

                var dayTotal = 0;

                // Production ticks every 3 hours; never seed into the future.
                for (var tick = 0; tick < 8; tick++)
                {
                    var at = dayStart.AddHours(tick * 3).AddMinutes(Rng.Next(0, 60));
                    if (at > now) continue;
                    var efficiency = 0.70 + Rng.NextDouble() * 0.25; // 70%-95%
                    var qty = (int)(m.IdealRunRatePerHour * 3 * efficiency);
                    dayTotal += qty;
                    production.Add(new ProductionRecord
                    {
                        MachineId = m.Id,
                        OperatorId = operators[Rng.Next(operators.Count)].Id,
                        Quantity = qty,
                        ProducedAt = at
                    });
                }

                if (dayTotal == 0) continue;

                // Downtimes for the day (weighted by machine profile), clamped to "now".
                var stops = Rng.Next(profile.MinDailyStops, profile.MaxDailyStops + 1);
                for (var s = 0; s < stops; s++)
                {
                    var reason = profile.PickReason();
                    var start = dayStart.AddHours(Rng.Next(0, 23)).AddMinutes(Rng.Next(0, 60));
                    if (start > now) continue;
                    var end = start.AddMinutes(Rng.Next(8, 45));
                    if (end > now) end = now;
                    downtimes.Add(new Downtime
                    {
                        MachineId = m.Id,
                        Reason = reason,
                        StartTime = start,
                        EndTime = end
                    });
                }

                // One quality sample per day (capped at now).
                var defects = (int)(dayTotal * profile.DefectRate * (0.5 + Rng.NextDouble()));
                var createdAt = dayStart.AddHours(23);
                if (createdAt > now) createdAt = now;
                quality.Add(new QualityRecord
                {
                    MachineId = m.Id,
                    ProducedQuantity = dayTotal,
                    DefectQuantity = defects,
                    CreatedAt = createdAt
                });
            }
        }

        db.ProductionRecords.AddRange(production);
        db.Downtimes.AddRange(downtimes);
        db.QualityRecords.AddRange(quality);
        await db.SaveChangesAsync();
    }

    private static SeedProfile GetProfile(Machine m)
    {
        // Dokuma-03 is intentionally the "problem child": many yarn breaks, to surface in AI insights.
        var isProblemWeaver = m is { MachineType: MachineType.Weaving } && m.Name.EndsWith("03");

        return m.MachineType switch
        {
            MachineType.Weaving => new SeedProfile(
                DefectRate: 0.03,
                MinDailyStops: isProblemWeaver ? 1 : 0,
                MaxDailyStops: isProblemWeaver ? 2 : 2,
                Weights: new() { [DowntimeReason.YarnBreak] = isProblemWeaver ? 8 : 4, [DowntimeReason.OperatorWaiting] = 2, [DowntimeReason.Maintenance] = 1, [DowntimeReason.PowerFailure] = 1 }),
            MachineType.Dyeing => new SeedProfile(
                DefectRate: 0.04,
                MinDailyStops: 0, MaxDailyStops: 2,
                Weights: new() { [DowntimeReason.MaterialWaiting] = 5, [DowntimeReason.Maintenance] = 2, [DowntimeReason.PowerFailure] = 1 }),
            _ => new SeedProfile(
                DefectRate: 0.02,
                MinDailyStops: 0, MaxDailyStops: 1,
                Weights: new() { [DowntimeReason.Maintenance] = 4, [DowntimeReason.OperatorWaiting] = 2, [DowntimeReason.MaterialWaiting] = 1 }),
        };
    }

    private sealed record SeedProfile(double DefectRate, int MinDailyStops, int MaxDailyStops, Dictionary<DowntimeReason, int> Weights)
    {
        public DowntimeReason PickReason()
        {
            var total = Weights.Values.Sum();
            var roll = Rng.Next(total);
            var acc = 0;
            foreach (var (reason, weight) in Weights)
            {
                acc += weight;
                if (roll < acc) return reason;
            }
            return Weights.Keys.First();
        }
    }
}
