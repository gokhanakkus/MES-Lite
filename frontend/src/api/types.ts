// TypeScript mirrors of the backend DTOs.

export type MachineStatus = 'Running' | 'Stopped' | 'Maintenance'
export type MachineType = 'Weaving' | 'Dyeing' | 'Cutting'
export type PeriodType = 'Daily' | 'Weekly' | 'Monthly'

export interface MachineDto {
  id: number
  name: string
  machineType: MachineType
  machineTypeName: string
  status: MachineStatus
  statusName: string
  productionLine: string
  idealRunRatePerHour: number
  currentOperator: string | null
  currentHourProduction: number
  oee: number
  // Live physical telemetry
  healthScore: number
  wearLevel: number
  rpm: number
  speed: number
  load: number
  vibration: number
  temperature: number
  efficiency: number
  cycleTimeSeconds: number
}

/** Payload pushed on the SignalR `MachineTelemetry` event (one entry per machine, every tick). */
export interface MachineTelemetry {
  machineId: number
  machineName: string
  status: MachineStatus
  healthScore: number
  wearLevel: number
  rpm: number
  speed: number
  load: number
  vibration: number
  temperature: number
  efficiency: number
  cycleTimeSeconds: number
}

export interface ProductionRecordDto {
  id: number
  machineId: number
  machineName: string
  operatorId: number | null
  operatorName: string | null
  quantity: number
  producedAt: string
}

export interface DowntimeDto {
  id: number
  machineId: number
  machineName: string
  reason: string
  reasonName: string
  startTime: string
  endTime: string | null
  durationMinutes: number
  isOngoing: boolean
}

export interface LabelCount {
  label: string
  count: number
}

export interface MachineDowntimeDto {
  machineId: number
  machineName: string
  stops: number
  totalMinutes: number
}

export interface DowntimeAnalyticsDto {
  byReason: LabelCount[]
  byMachine: MachineDowntimeDto[]
  recent: DowntimeDto[]
}

export interface QualityRecordDto {
  id: number
  machineId: number
  machineName: string
  producedQuantity: number
  defectQuantity: number
  qualityRate: number
  createdAt: string
}

export interface QualitySummaryDto {
  totalProduced: number
  totalDefects: number
  qualityPercentage: number
  defectsByMachine: LabelCount[]
  recent: QualityRecordDto[]
}

export interface OeeDto {
  machineId: number
  machineName: string
  availability: number
  performance: number
  quality: number
  oee: number
}

export interface OeeDashboardDto {
  averageOee: number
  averageAvailability: number
  averagePerformance: number
  averageQuality: number
  machines: OeeDto[]
}

export interface DashboardKpiDto {
  totalMachines: number
  runningMachines: number
  stoppedMachines: number
  maintenanceMachines: number
  todayProduction: number
  averageOee: number
}

export interface OperatorPerformanceDto {
  operatorId: number
  fullName: string
  shift: string
  totalProduction: number
  totalDowntimeMinutes: number
  averageOee: number
}

export interface MaintenanceInsightDto {
  machineId: number
  machineName: string
  topDowntimeReason: string
  topReasonCount: number
  totalStops: number
  totalDowntimeHours: number
  severity: string
  recommendation: string
}

export interface ReportRowDto {
  machineId: number
  machineName: string
  machineType: string
  totalProduction: number
  totalDefects: number
  availability: number
  performance: number
  quality: number
  oee: number
  downtimeHours: number
}

export interface ReportDto {
  period: string
  from: string
  to: string
  rows: ReportRowDto[]
}

export interface TelemetryReading {
  machineId: number
  createdAt: string
  healthScore: number
  wearLevel: number
  vibration: number
  temperature: number
  rpm: number
  speed: number
  load: number
}

export type AlarmSeverity = 'Warning' | 'Critical'

export interface AlarmDto {
  id: number
  machineId: number
  machineName: string
  metric: string
  severity: AlarmSeverity
  detector: string
  message: string
  value: number
  limit: number
  raisedAt: string
  resolvedAt: string | null
  isActive: boolean
}

export interface RulDto {
  machineId: number
  machineName: string
  currentHealth: number
  currentWear: number
  wearRatePerHour: number
  remainingHours: number | null
  predictedFailureAt: string | null
  status: string
}
