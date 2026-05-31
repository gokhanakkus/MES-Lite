using MESLite.Application.Common.Interfaces;
using MESLite.Application.Common.Models;
using MESLite.Domain.Entities;
using MESLite.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MESLite.Simulator;

/// <summary>
/// The heart of MES Lite: a hosted background service that, every few seconds, advances the
/// virtual shop floor — producing output, randomly stopping machines, recovering them, recording
/// quality samples — and broadcasts every change over SignalR so the UI updates without a refresh.
/// </summary>
public sealed class ProductionSimulatorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SimulatorOptions _options;
    private readonly ILogger<ProductionSimulatorService> _logger;
    private readonly Random _rng = new();
    private readonly AlarmEvaluator _alarmEvaluator = new();
    // Active alarm DB ids keyed by (machineId, metric) — drives the raised/resolved lifecycle.
    private readonly Dictionary<(int MachineId, AlarmMetric Metric), int> _activeAlarms = new();

    // Recovery likelihoods per tick.
    private const double ResumeProbability = 0.40;
    private const double MaintenanceRecoverProbability = 0.25;

    /// <summary>How long telemetry snapshots are retained before being pruned.</summary>
    private const int RetentionHours = 6;

    private long _tick;

    public ProductionSimulatorService(
        IServiceScopeFactory scopeFactory,
        IOptions<SimulatorOptions> options,
        ILogger<ProductionSimulatorService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Production simulator is disabled via configuration.");
            return;
        }

        _logger.LogInformation("Production simulator started (interval {Interval}s).", _options.IntervalSeconds);

        // Give the host a moment so the DB is migrated/seeded before the first tick.
        try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); } catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds)));
        while (await SafeWaitAsync(timer, stoppingToken))
        {
            try
            {
                await RunTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Simulator tick failed; continuing.");
            }
        }

        _logger.LogInformation("Production simulator stopped.");
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task RunTickAsync(CancellationToken ct)
    {
        _tick++;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var notifier = scope.ServiceProvider.GetRequiredService<IProductionNotifier>();

        var machines = await db.Machines.ToListAsync(ct);
        var operatorIds = await db.Operators.Select(o => o.Id).ToListAsync(ct);

        var ongoing = await db.Downtimes
            .Where(d => d.EndTime == null)
            .ToListAsync(ct);
        var ongoingByMachine = ongoing.ToDictionary(d => d.MachineId);

        var now = DateTime.UtcNow;

        foreach (var machine in machines)
        {
            var profile = MachineSimulationProfile.For(machine.MachineType);

            switch (machine.Status)
            {
                case MachineStatus.Maintenance:
                    MachineTelemetrySimulator.StepIdle(machine, _rng);
                    await TryRecoverAsync(machine, profile, ongoingByMachine, now, notifier, ct);
                    break;

                case MachineStatus.Stopped:
                    MachineTelemetrySimulator.StepIdle(machine, _rng);
                    await TryResumeAsync(machine, ongoingByMachine, now, notifier, ct);
                    break;

                case MachineStatus.Running:
                default:
                    await StepRunningMachineAsync(db, machine, profile, operatorIds, now, notifier, ct);
                    break;
            }
        }

        // Persist a telemetry snapshot per machine (time series for trends + RUL prediction).
        foreach (var m in machines)
        {
            db.TelemetrySnapshots.Add(new MachineTelemetrySnapshot
            {
                MachineId = m.Id,
                CreatedAt = now,
                HealthScore = m.HealthScore,
                WearLevel = m.WearLevel,
                Rpm = m.Rpm,
                Speed = m.Speed,
                Load = m.Load,
                Vibration = m.Vibration,
                Temperature = m.Temperature,
                Efficiency = m.Efficiency
            });
        }

        await db.SaveChangesAsync(ct);

        // Keep the time series bounded: drop snapshots older than the retention window periodically.
        if (_tick % 120 == 0)
        {
            await db.TelemetrySnapshots
                .Where(t => t.CreatedAt < now.AddHours(-RetentionHours))
                .ExecuteDeleteAsync(ct);
        }

        // Evaluate anomaly detection and drive the alarm lifecycle.
        await ProcessAlarmsAsync(db, machines, notifier, now, ct);

        // Broadcast live physical telemetry every tick (this is the "sensor stream").
        await BroadcastTelemetryAsync(machines, notifier, ct);

        if (_tick % Math.Max(1, _options.OeeBroadcastEveryTicks) == 0)
        {
            await BroadcastOeeAsync(scope, notifier, ct);
        }
    }

    private async Task StepRunningMachineAsync(
        IApplicationDbContext db, Machine machine, MachineSimulationProfile profile,
        IReadOnlyList<int> operatorIds, DateTime now, IProductionNotifier notifier, CancellationToken ct)
    {
        // 0) Evolve physical telemetry (load, rpm, wear, vibration, temperature, health, speed).
        MachineTelemetrySimulator.StepRunning(machine, profile, _rng);

        // 1) Maintenance — triggered by condition (probability climbs as health drops; forced at 0).
        if (machine.HealthScore <= 0 || _rng.NextDouble() < MachineTelemetrySimulator.EffectiveMaintenanceProbability(machine, profile))
        {
            db.Downtimes.Add(new Downtime { MachineId = machine.Id, Reason = DowntimeReason.Maintenance, StartTime = now });
            machine.Status = MachineStatus.Maintenance;
            await notifier.MachineStatusChangedAsync(new { machineId = machine.Id, machineName = machine.Name, status = machine.Status.ToString() }, ct);
            await notifier.DowntimeCreatedAsync(new { machineId = machine.Id, machineName = machine.Name, reason = DowntimeReason.Maintenance.ToString(), startTime = now }, ct);
            return;
        }

        // 2) Unplanned stop — probability amplified by wear / vibration / temperature.
        if (_rng.NextDouble() < MachineTelemetrySimulator.EffectiveStopProbability(machine, profile))
        {
            var reason = profile.PickReason(_rng);
            db.Downtimes.Add(new Downtime { MachineId = machine.Id, Reason = reason, StartTime = now });
            machine.Status = MachineStatus.Stopped;
            await notifier.MachineStatusChangedAsync(new { machineId = machine.Id, machineName = machine.Name, status = machine.Status.ToString() }, ct);
            await notifier.DowntimeCreatedAsync(new { machineId = machine.Id, machineName = machine.Name, reason = reason.ToString(), startTime = now }, ct);
            return;
        }

        // 3) Produce at the current physical speed (units/hour) over the elapsed interval. Speed already
        //    reflects health-adjusted efficiency, so worn machines genuinely produce less. Probabilistic
        //    rounding keeps the long-run average exact even though per-tick output is fractional.
        var expected = machine.Speed * (_options.IntervalSeconds / 3600.0) * _options.SpeedFactor;
        var quantity = (int)Math.Floor(expected);
        if (_rng.NextDouble() < expected - quantity) quantity++;
        if (quantity <= 0) return; // produced less than one unit this interval

        var operatorId = operatorIds.Count > 0 ? operatorIds[_rng.Next(operatorIds.Count)] : (int?)null;

        db.ProductionRecords.Add(new ProductionRecord
        {
            MachineId = machine.Id,
            OperatorId = operatorId,
            Quantity = quantity,
            ProducedAt = now
        });

        var defects = (int)Math.Round(quantity * profile.DefectRate * (0.4 + _rng.NextDouble()));
        db.QualityRecords.Add(new QualityRecord
        {
            MachineId = machine.Id,
            ProducedQuantity = quantity,
            DefectQuantity = Math.Min(defects, quantity),
            CreatedAt = now
        });

        await notifier.ProductionUpdatedAsync(new
        {
            machineId = machine.Id,
            machineName = machine.Name,
            machineType = machine.MachineType.ToString(),
            quantity,
            producedAt = now,
            status = machine.Status.ToString()
        }, ct);
    }

    private async Task TryResumeAsync(Machine machine, IReadOnlyDictionary<int, Downtime> ongoing, DateTime now, IProductionNotifier notifier, CancellationToken ct)
    {
        if (_rng.NextDouble() >= ResumeProbability) return;

        if (ongoing.TryGetValue(machine.Id, out var dt)) dt.EndTime = now;
        machine.Status = MachineStatus.Running;
        await notifier.MachineStatusChangedAsync(new { machineId = machine.Id, machineName = machine.Name, status = machine.Status.ToString() }, ct);
    }

    private async Task TryRecoverAsync(Machine machine, MachineSimulationProfile profile, IReadOnlyDictionary<int, Downtime> ongoing, DateTime now, IProductionNotifier notifier, CancellationToken ct)
    {
        if (_rng.NextDouble() >= MaintenanceRecoverProbability) return;

        if (ongoing.TryGetValue(machine.Id, out var dt)) dt.EndTime = now;
        // Maintenance refurbishes the machine: wear cleared, condition restored.
        MachineTelemetrySimulator.ApplyMaintenance(machine, profile);
        machine.Status = MachineStatus.Running;
        await notifier.MachineStatusChangedAsync(new { machineId = machine.Id, machineName = machine.Name, status = machine.Status.ToString() }, ct);
    }

    private async Task ProcessAlarmsAsync(
        IApplicationDbContext db, IReadOnlyList<Machine> machines, IProductionNotifier notifier, DateTime now, CancellationToken ct)
    {
        var newAlarms = new List<((int, AlarmMetric) Key, Alarm Alarm)>();
        var resolvedAlarms = new List<Alarm>();

        foreach (var machine in machines)
        {
            var conditions = _alarmEvaluator.Evaluate(machine, machine.Status == MachineStatus.Running);
            var activeMetrics = conditions.Select(c => c.Metric).ToHashSet();

            // Raise newly-triggered conditions.
            foreach (var c in conditions)
            {
                var key = (machine.Id, c.Metric);
                if (_activeAlarms.ContainsKey(key)) continue;

                var alarm = new Alarm
                {
                    MachineId = machine.Id,
                    Metric = c.Metric,
                    Severity = c.Severity,
                    Detector = c.Detector,
                    Message = c.Message,
                    Value = Math.Round(c.Value, 2),
                    Limit = Math.Round(c.Limit, 2),
                    RaisedAt = now
                };
                db.Alarms.Add(alarm);
                newAlarms.Add((key, alarm));
            }

            // Resolve conditions that have cleared for this machine.
            var clearedKeys = _activeAlarms.Keys
                .Where(k => k.MachineId == machine.Id && !activeMetrics.Contains(k.Metric))
                .ToList();
            foreach (var key in clearedKeys)
            {
                var id = _activeAlarms[key];
                var alarm = await db.Alarms.FirstOrDefaultAsync(a => a.Id == id, ct);
                if (alarm is { ResolvedAt: null })
                {
                    alarm.ResolvedAt = now;
                    resolvedAlarms.Add(alarm);
                }
                _activeAlarms.Remove(key);
            }
        }

        if (newAlarms.Count == 0 && resolvedAlarms.Count == 0) return;

        await db.SaveChangesAsync(ct); // assigns ids to new alarms, persists resolutions

        var machineNames = machines.ToDictionary(m => m.Id, m => m.Name);
        foreach (var (key, alarm) in newAlarms)
        {
            _activeAlarms[key] = alarm.Id;
            await notifier.AlarmRaisedAsync(new
            {
                id = alarm.Id,
                machineId = alarm.MachineId,
                machineName = machineNames.GetValueOrDefault(alarm.MachineId, ""),
                metric = alarm.Metric.ToString(),
                severity = alarm.Severity.ToString(),
                detector = alarm.Detector.ToString(),
                message = alarm.Message,
                value = alarm.Value,
                limit = alarm.Limit,
                raisedAt = alarm.RaisedAt
            }, ct);
        }

        foreach (var alarm in resolvedAlarms)
        {
            await notifier.AlarmResolvedAsync(new
            {
                id = alarm.Id,
                machineId = alarm.MachineId,
                metric = alarm.Metric.ToString(),
                resolvedAt = alarm.ResolvedAt
            }, ct);
        }
    }

    private static async Task BroadcastTelemetryAsync(IReadOnlyList<Machine> machines, IProductionNotifier notifier, CancellationToken ct)
    {
        var payload = machines.Select(m => new
        {
            machineId = m.Id,
            machineName = m.Name,
            status = m.Status.ToString(),
            healthScore = Math.Round(m.HealthScore, 1),
            wearLevel = Math.Round(m.WearLevel, 1),
            rpm = m.Rpm,
            speed = Math.Round(m.Speed, 1),
            load = Math.Round(m.Load, 1),
            vibration = Math.Round(m.Vibration, 2),
            temperature = Math.Round(m.Temperature, 1),
            efficiency = Math.Round(m.Efficiency * 100, 1),
            cycleTimeSeconds = Math.Round(m.CycleTimeSeconds, 2)
        }).ToList();

        await notifier.MachineTelemetryAsync(new { machines = payload }, ct);
    }

    private static async Task BroadcastOeeAsync(IServiceScope scope, IProductionNotifier notifier, CancellationToken ct)
    {
        var oeeService = scope.ServiceProvider.GetRequiredService<IOeeCalculationService>();
        var (from, to) = PeriodType.Daily.ToRange(DateTime.UtcNow);
        var results = await oeeService.CalculateForAllAsync(from, to, ct);

        var payload = results.Select(kv => new
        {
            machineId = kv.Key,
            availability = Math.Round(kv.Value.Availability * 100, 1),
            performance = Math.Round(kv.Value.Performance * 100, 1),
            quality = Math.Round(kv.Value.Quality * 100, 1),
            oee = Math.Round(kv.Value.Oee * 100, 1)
        }).ToList();

        var average = payload.Count == 0 ? 0 : Math.Round(payload.Average(p => p.oee), 1);
        await notifier.OeeUpdatedAsync(new { averageOee = average, machines = payload }, ct);
    }
}
