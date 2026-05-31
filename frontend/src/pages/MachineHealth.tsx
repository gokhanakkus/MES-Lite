import { Box, Card, CardContent, Chip, LinearProgress, Stack, Tooltip, Typography } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import type { MachineTelemetry, MachineType, RulDto } from '../api/types'
import { HealthGauge } from '../components/HealthGauge'
import { Sparkline } from '../components/Sparkline'
import { StatusChip } from '../components/StatusChip'
import { useHubEvent } from '../realtime/signalr'
import { healthLabel } from '../theme'

const MAX_POINTS = 120
const unitFor = (t?: MachineType) => (t === 'Cutting' ? 'adet/sa' : 'm/sa')

interface Series {
  vibration: number[]
  temperature: number[]
}

const rulColor = (status: string) => (status === 'Kritik' ? 'error' : status === 'Uyarı' ? 'warning' : 'success')

function formatRul(r?: RulDto): string {
  if (!r) return '—'
  if (r.remainingHours == null) return 'Kalan ömür: yeterli'
  const h = r.remainingHours
  return h < 1 ? `Kalan ömür: ~${Math.round(h * 60)} dk` : `Kalan ömür: ~${h.toFixed(1)} sa`
}

function Metric({ label, value, unit }: { label: string; value: string | number; unit?: string }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="subtitle1" sx={{ fontWeight: 700, lineHeight: 1.2 }}>
        {value}
        {unit && (
          <Typography component="span" variant="caption" sx={{ color: 'text.secondary', ml: 0.5 }}>
            {unit}
          </Typography>
        )}
      </Typography>
    </Box>
  )
}

function Bar({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
        <Typography variant="caption" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="caption" sx={{ color }}>
          {Math.round(value)}%
        </Typography>
      </Stack>
      <LinearProgress
        variant="determinate"
        value={Math.min(100, Math.max(0, value))}
        sx={{ height: 6, borderRadius: 3, '& .MuiLinearProgress-bar': { backgroundColor: color } }}
      />
    </Box>
  )
}

export function MachineHealth() {
  const navigate = useNavigate()
  const machines = useQuery({ queryKey: ['machines'], queryFn: api.machines })
  const history = useQuery({ queryKey: ['telemetry', 30], queryFn: () => api.recentTelemetry(30) })
  const rul = useQuery({ queryKey: ['rul'], queryFn: () => api.rul(30), refetchInterval: 15000 })

  const [telemetry, setTelemetry] = useState<Record<number, MachineTelemetry>>({})
  const [types, setTypes] = useState<Record<number, MachineType>>({})
  const [series, setSeries] = useState<Record<number, Series>>({})
  const seriesSeeded = useRef(false)

  // Current values + machine types from REST.
  useEffect(() => {
    if (!machines.data) return
    const tel: Record<number, MachineTelemetry> = {}
    const ty: Record<number, MachineType> = {}
    for (const m of machines.data) {
      ty[m.id] = m.machineType
      tel[m.id] = {
        machineId: m.id, machineName: m.name, status: m.status,
        healthScore: m.healthScore, wearLevel: m.wearLevel, rpm: m.rpm, speed: m.speed,
        load: m.load, vibration: m.vibration, temperature: m.temperature,
        efficiency: m.efficiency, cycleTimeSeconds: m.cycleTimeSeconds,
      }
    }
    setTypes(ty)
    setTelemetry((prev) => ({ ...tel, ...prev }))
  }, [machines.data])

  // Seed trend series from history (once).
  useEffect(() => {
    if (!history.data || seriesSeeded.current) return
    const s: Record<number, Series> = {}
    for (const r of history.data) {
      ;(s[r.machineId] ??= { vibration: [], temperature: [] })
      s[r.machineId].vibration.push(Math.round(r.vibration * 100) / 100)
      s[r.machineId].temperature.push(Math.round(r.temperature * 10) / 10)
    }
    for (const id of Object.keys(s)) {
      const k = Number(id)
      s[k].vibration = s[k].vibration.slice(-MAX_POINTS)
      s[k].temperature = s[k].temperature.slice(-MAX_POINTS)
    }
    setSeries(s)
    seriesSeeded.current = true
  }, [history.data])

  // Live updates: current values + append to trend series.
  useHubEvent<{ machines: MachineTelemetry[] }>('MachineTelemetry', (payload) => {
    setTelemetry((prev) => {
      const next = { ...prev }
      for (const t of payload.machines) next[t.machineId] = t
      return next
    })
    setSeries((prev) => {
      const next = { ...prev }
      for (const t of payload.machines) {
        const cur = next[t.machineId] ?? { vibration: [], temperature: [] }
        next[t.machineId] = {
          vibration: [...cur.vibration, t.vibration].slice(-MAX_POINTS),
          temperature: [...cur.temperature, t.temperature].slice(-MAX_POINTS),
        }
      }
      return next
    })
  })

  const rulById: Record<number, RulDto> = Object.fromEntries((rul.data ?? []).map((r) => [r.machineId, r]))
  const items = Object.values(telemetry).sort((a, b) => a.machineName.localeCompare(b.machineName))

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Makine Sağlığı
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Canlı fiziksel parametreler, titreşim/sıcaklık trendleri ve kalan ömür (RUL) tahmini — SignalR ile gerçek zamanlı
      </Typography>

      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', lg: 'repeat(3, 1fr)' } }}>
        {items.map((t) => {
          const r = rulById[t.machineId]
          const s = series[t.machineId] ?? { vibration: [], temperature: [] }
          const vibColor = t.vibration > 6 ? '#ef5350' : t.vibration > 4 ? '#ffa726' : '#26c6da'
          const tempColor = t.temperature > 85 ? '#ef5350' : t.temperature > 75 ? '#ffa726' : '#42a5f5'
          const wearColor = t.wearLevel > 60 ? '#ef5350' : t.wearLevel > 40 ? '#ffa726' : '#42a5f5'
          return (
            <Card
              key={t.machineId}
              onClick={() => navigate(`/machines/${t.machineId}`)}
              sx={{ cursor: 'pointer', transition: 'border-color .15s', '&:hover': { borderColor: 'primary.main' } }}
            >
              <CardContent>
                <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                  <Box>
                    <Typography variant="h6">{t.machineName}</Typography>
                    <Chip size="small" label={healthLabel(t.healthScore)} variant="outlined" sx={{ mt: 0.5 }} />
                  </Box>
                  <StatusChip status={t.status} />
                </Stack>

                <Stack direction="row" spacing={2} sx={{ alignItems: 'center', mb: 1.5 }}>
                  <HealthGauge value={t.healthScore} />
                  <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1.5, flexGrow: 1 }}>
                    <Metric label="RPM" value={t.rpm} />
                    <Metric label="Hız" value={t.speed} unit={unitFor(types[t.machineId])} />
                    <Metric label="Verim" value={`${t.efficiency}%`} />
                    <Metric label="Cycle" value={t.cycleTimeSeconds} unit="sn" />
                  </Box>
                </Stack>

                {r && (
                  <Tooltip
                    title={
                      r.remainingHours == null
                        ? 'Aşınma trendi düşük/negatif — yakın bir arıza öngörülmüyor'
                        : `Aşınma hızı ~${r.wearRatePerHour}/saat · tahmini: ${r.predictedFailureAt ? new Date(r.predictedFailureAt).toLocaleString('tr-TR') : '—'}`
                    }
                  >
                    <Chip
                      size="small"
                      color={rulColor(r.status)}
                      variant="outlined"
                      label={`${r.status} · ${formatRul(r)}`}
                      sx={{ mb: 1.5, fontWeight: 600 }}
                    />
                  </Tooltip>
                )}

                <Stack spacing={1.2} sx={{ mb: 1.5 }}>
                  <Bar label="Yük (Load)" value={t.load} color="#42a5f5" />
                  <Bar label="Aşınma (Wear)" value={t.wearLevel} color={wearColor} />
                </Stack>

                <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                  <Sparkline label="Titreşim (mm/s)" data={s.vibration} color={vibColor} />
                  <Sparkline label="Sıcaklık (°C)" data={s.temperature} color={tempColor} />
                </Box>
              </CardContent>
            </Card>
          )
        })}
      </Box>
    </Box>
  )
}
