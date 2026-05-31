using MESLite.Domain.Enums;

namespace MESLite.Application.Common.Dtos;

// Lightweight read models returned by queries and pushed over SignalR.
// Records keep them immutable and serialization-friendly.

public sealed record MachineDto(
    int Id,
    string Name,
    MachineType MachineType,
    string MachineTypeName,
    MachineStatus Status,
    string StatusName,
    string ProductionLine,
    int IdealRunRatePerHour,
    string? CurrentOperator,
    int CurrentHourProduction,
    double Oee,
    // Live physical telemetry
    double HealthScore,
    double WearLevel,
    int Rpm,
    double Speed,
    double Load,
    double Vibration,
    double Temperature,
    double Efficiency,
    double CycleTimeSeconds);

public sealed record ProductionRecordDto(
    int Id,
    int MachineId,
    string MachineName,
    int? OperatorId,
    string? OperatorName,
    int Quantity,
    DateTime ProducedAt);

public sealed record DowntimeDto(
    int Id,
    int MachineId,
    string MachineName,
    DowntimeReason Reason,
    string ReasonName,
    DateTime StartTime,
    DateTime? EndTime,
    double DurationMinutes,
    bool IsOngoing);

public sealed record QualityRecordDto(
    int Id,
    int MachineId,
    string MachineName,
    int ProducedQuantity,
    int DefectQuantity,
    double QualityRate,
    DateTime CreatedAt);

public sealed record QualitySummaryDto(
    int TotalProduced,
    int TotalDefects,
    double QualityPercentage,
    IReadOnlyList<DefectByReasonDto> DefectsByMachine,
    IReadOnlyList<QualityRecordDto> Recent);

public sealed record DefectByReasonDto(string Label, int Count);

public sealed record OeeDto(
    int MachineId,
    string MachineName,
    double Availability,
    double Performance,
    double Quality,
    double Oee);

public sealed record OeeDashboardDto(
    double AverageOee,
    double AverageAvailability,
    double AveragePerformance,
    double AverageQuality,
    IReadOnlyList<OeeDto> Machines);

public sealed record DashboardKpiDto(
    int TotalMachines,
    int RunningMachines,
    int StoppedMachines,
    int MaintenanceMachines,
    int TodayProduction,
    double AverageOee);

public sealed record OperatorPerformanceDto(
    int OperatorId,
    string FullName,
    string Shift,
    int TotalProduction,
    double TotalDowntimeMinutes,
    double AverageOee);

public sealed record MaintenanceInsightDto(
    int MachineId,
    string MachineName,
    string TopDowntimeReason,
    int TopReasonCount,
    int TotalStops,
    double TotalDowntimeHours,
    string Severity,
    string Recommendation);

public sealed record DowntimeAnalyticsDto(
    IReadOnlyList<DefectByReasonDto> ByReason,
    IReadOnlyList<MachineDowntimeDto> ByMachine,
    IReadOnlyList<DowntimeDto> Recent);

public sealed record MachineDowntimeDto(int MachineId, string MachineName, int Stops, double TotalMinutes);

public sealed record ReportRowDto(
    int MachineId,
    string MachineName,
    string MachineType,
    int TotalProduction,
    int TotalDefects,
    double Availability,
    double Performance,
    double Quality,
    double Oee,
    double DowntimeHours);

public sealed record ReportDto(
    string Period,
    DateTime From,
    DateTime To,
    IReadOnlyList<ReportRowDto> Rows);

public sealed record TelemetryReadingDto(
    int MachineId,
    DateTime CreatedAt,
    double HealthScore,
    double WearLevel,
    double Vibration,
    double Temperature,
    int Rpm,
    double Speed,
    double Load);

public sealed record AlarmDto(
    int Id,
    int MachineId,
    string MachineName,
    string Metric,
    string Severity,
    string Detector,
    string Message,
    double Value,
    double Limit,
    DateTime RaisedAt,
    DateTime? ResolvedAt,
    bool IsActive);

/// <summary>Remaining Useful Life estimate for a machine, from the recent wear trend.</summary>
public sealed record RulDto(
    int MachineId,
    string MachineName,
    double CurrentHealth,
    double CurrentWear,
    double WearRatePerHour,
    double? RemainingHours,
    DateTime? PredictedFailureAt,
    string Status);
