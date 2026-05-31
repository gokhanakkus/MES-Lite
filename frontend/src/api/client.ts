import axios from 'axios'
import type {
  AlarmDto,
  DashboardKpiDto,
  DowntimeAnalyticsDto,
  MachineDto,
  MaintenanceInsightDto,
  OeeDashboardDto,
  OperatorPerformanceDto,
  PeriodType,
  ProductionRecordDto,
  QualitySummaryDto,
  ReportDto,
  RulDto,
  TelemetryReading,
} from './types'

// In dev, Vite proxies "/api" to the backend. In prod, set VITE_API_BASE.
const baseURL = import.meta.env.VITE_API_BASE ?? ''

export const http = axios.create({ baseURL })

export const api = {
  dashboardKpis: () => http.get<DashboardKpiDto>('/api/dashboard/kpis').then((r) => r.data),
  machines: () => http.get<MachineDto[]>('/api/machines').then((r) => r.data),
  machine: (id: number) => http.get<MachineDto>(`/api/machines/${id}`).then((r) => r.data),
  liveProduction: (take = 50) =>
    http.get<ProductionRecordDto[]>(`/api/production/live?take=${take}`).then((r) => r.data),
  downtimes: (days = 7) =>
    http.get<DowntimeAnalyticsDto>(`/api/downtimes?days=${days}`).then((r) => r.data),
  quality: (days = 7) =>
    http.get<QualitySummaryDto>(`/api/quality?days=${days}`).then((r) => r.data),
  oeeDashboard: (period: PeriodType = 'Daily') =>
    http.get<OeeDashboardDto>(`/api/oee/dashboard?period=${period}`).then((r) => r.data),
  operatorPerformance: (days = 7) =>
    http.get<OperatorPerformanceDto[]>(`/api/operators/performance?days=${days}`).then((r) => r.data),
  report: (period: PeriodType = 'Daily') =>
    http.get<ReportDto>(`/api/reports?period=${period}`).then((r) => r.data),
  maintenanceInsights: (days = 30) =>
    http.get<MaintenanceInsightDto[]>(`/api/analytics/maintenance?days=${days}`).then((r) => r.data),
  rul: (windowMinutes = 30) =>
    http.get<RulDto[]>(`/api/analytics/rul?windowMinutes=${windowMinutes}`).then((r) => r.data),
  recentTelemetry: (minutes = 30) =>
    http.get<TelemetryReading[]>(`/api/telemetry/recent?minutes=${minutes}`).then((r) => r.data),
  machineTelemetry: (machineId: number, minutes = 60) =>
    http.get<TelemetryReading[]>(`/api/telemetry/${machineId}?minutes=${minutes}`).then((r) => r.data),
  alarms: (activeOnly = false, hours = 24) =>
    http.get<AlarmDto[]>(`/api/alarms?activeOnly=${activeOnly}&hours=${hours}`).then((r) => r.data),
  reportDownloadUrl: (format: 'csv' | 'excel', period: PeriodType) =>
    `${baseURL}/api/reports/${format}?period=${period}`,
}
