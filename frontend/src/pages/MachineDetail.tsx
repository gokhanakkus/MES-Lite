import { Box, Button, Card, CardContent, Chip, LinearProgress, Stack, Typography } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { useQuery } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import {
  Area,
  AreaChart,
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { api } from '../api/client'
import type { MachineTelemetry, MachineType } from '../api/types'
import { HealthGauge } from '../components/HealthGauge'
import { StatusChip } from '../components/StatusChip'
import { useHubEvent } from '../realtime/signalr'
import { healthLabel, oeeColor } from '../theme'

const MAX_POINTS = 240
const typeLabels: Record<MachineType, string> = { Weaving: 'Dokuma', Dyeing: 'Boya', Cutting: 'Kesim' }
const unitFor = (t?: MachineType) => (t === 'Cutting' ? 'adet/sa' : 'm/sa')
const rulColor = (s: string) => (s === 'Kritik' ? 'error' : s === 'Uyarı' ? 'warning' : 'success')

interface Point {
  label: string
  health: number
  vibration: number
  temperature: number
  wear: number
}

function Metric({ label, value, unit, color }: { label: string; value: string | number; unit?: string; color?: string }) {
  return (
    <Box>
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h6" sx={{ fontWeight: 700, color, lineHeight: 1.3 }}>
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

function TrendChart({
  title,
  data,
  dataKey,
  color,
  unit,
  domain,
  area,
}: {
  title: string
  data: Point[]
  dataKey: keyof Point
  color: string
  unit?: string
  domain?: [number | 'auto', number | 'auto']
  area?: boolean
}) {
  return (
    <Card>
      <CardContent>
        <Typography variant="subtitle1" sx={{ fontWeight: 600, mb: 1 }}>
          {title}
          {unit && (
            <Typography component="span" variant="caption" sx={{ color: 'text.secondary', ml: 0.5 }}>
              ({unit})
            </Typography>
          )}
        </Typography>
        <Box sx={{ height: 220 }}>
          <ResponsiveContainer width="100%" height="100%">
            {area ? (
              <AreaChart data={data} margin={{ top: 6, right: 12, bottom: 6, left: -12 }}>
                <defs>
                  <linearGradient id={`grad-${dataKey}`} x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor={color} stopOpacity={0.4} />
                    <stop offset="100%" stopColor={color} stopOpacity={0.02} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.07)" />
                <XAxis dataKey="label" tick={{ fontSize: 10, fill: '#9aa7bd' }} minTickGap={48} />
                <YAxis domain={domain ?? ['auto', 'auto']} tick={{ fontSize: 11, fill: '#9aa7bd' }} />
                <Tooltip contentStyle={{ background: '#161d2e', border: '1px solid rgba(255,255,255,0.1)' }} />
                <Area type="monotone" dataKey={dataKey} stroke={color} strokeWidth={2} fill={`url(#grad-${dataKey})`} isAnimationActive={false} />
              </AreaChart>
            ) : (
              <LineChart data={data} margin={{ top: 6, right: 12, bottom: 6, left: -12 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.07)" />
                <XAxis dataKey="label" tick={{ fontSize: 10, fill: '#9aa7bd' }} minTickGap={48} />
                <YAxis domain={domain ?? ['auto', 'auto']} tick={{ fontSize: 11, fill: '#9aa7bd' }} />
                <Tooltip contentStyle={{ background: '#161d2e', border: '1px solid rgba(255,255,255,0.1)' }} />
                <Line type="monotone" dataKey={dataKey} stroke={color} strokeWidth={2} dot={false} isAnimationActive={false} />
              </LineChart>
            )}
          </ResponsiveContainer>
        </Box>
      </CardContent>
    </Card>
  )
}

export function MachineDetail() {
  const { id } = useParams()
  const machineId = Number(id)

  const machine = useQuery({ queryKey: ['machine', machineId], queryFn: () => api.machine(machineId), refetchInterval: 10000 })
  const history = useQuery({ queryKey: ['telemetry', 'machine', machineId], queryFn: () => api.machineTelemetry(machineId, 60) })
  const rul = useQuery({ queryKey: ['rul'], queryFn: () => api.rul(30), refetchInterval: 15000 })

  const [live, setLive] = useState<MachineTelemetry | null>(null)
  const [points, setPoints] = useState<Point[]>([])
  const seeded = useRef(false)

  useEffect(() => {
    if (!history.data || seeded.current) return
    setPoints(
      history.data.map((r) => ({
        label: new Date(r.createdAt).toLocaleTimeString('tr-TR'),
        health: r.healthScore,
        vibration: r.vibration,
        temperature: r.temperature,
        wear: r.wearLevel,
      })),
    )
    seeded.current = true
  }, [history.data])

  useHubEvent<{ machines: MachineTelemetry[] }>('MachineTelemetry', (payload) => {
    const t = payload.machines.find((m) => m.machineId === machineId)
    if (!t) return
    setLive(t)
    setPoints((prev) =>
      [
        ...prev,
        {
          label: new Date().toLocaleTimeString('tr-TR'),
          health: t.healthScore,
          vibration: t.vibration,
          temperature: t.temperature,
          wear: t.wearLevel,
        },
      ].slice(-MAX_POINTS),
    )
  })

  const m = machine.data
  const r = (rul.data ?? []).find((x) => x.machineId === machineId)

  // Prefer live telemetry values; fall back to the REST snapshot.
  const health = live?.healthScore ?? m?.healthScore ?? 0
  const vibration = live?.vibration ?? m?.vibration ?? 0
  const temperature = live?.temperature ?? m?.temperature ?? 0
  const wear = live?.wearLevel ?? m?.wearLevel ?? 0
  const rpm = live?.rpm ?? m?.rpm ?? 0
  const speed = live?.speed ?? m?.speed ?? 0
  const load = live?.load ?? m?.load ?? 0
  const efficiency = live?.efficiency ?? m?.efficiency ?? 0
  const cycle = live?.cycleTimeSeconds ?? m?.cycleTimeSeconds ?? 0
  const status = live?.status ?? m?.status ?? 'Running'

  return (
    <Box>
      <Button component={Link} to="/machines" startIcon={<ArrowBackIcon />} sx={{ mb: 1 }}>
        Makineler
      </Button>

      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 1, mb: 3 }}>
        <Box>
          <Typography variant="h4">{m?.name ?? `Makine #${machineId}`}</Typography>
          <Typography variant="body2" color="text.secondary">
            {m ? `${typeLabels[m.machineType]} · ${m.productionLine} · İdeal hız ${m.idealRunRatePerHour} ${unitFor(m.machineType)}` : '—'}
          </Typography>
        </Box>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          {r && <Chip color={rulColor(r.status)} variant="outlined" label={`RUL: ${r.remainingHours == null ? 'yeterli' : r.remainingHours < 1 ? `~${Math.round(r.remainingHours * 60)} dk` : `~${r.remainingHours.toFixed(1)} sa`}`} sx={{ fontWeight: 600 }} />}
          <StatusChip status={status} />
        </Stack>
      </Stack>

      {/* Snapshot strip */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Stack direction="row" spacing={3} sx={{ alignItems: 'center', flexWrap: 'wrap', rowGap: 2 }}>
            <Stack sx={{ alignItems: 'center' }} spacing={0.5}>
              <HealthGauge value={health} size={110} />
              <Chip size="small" label={healthLabel(health)} variant="outlined" />
            </Stack>
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(3, 1fr)', md: 'repeat(4, 1fr)' }, gap: 2.5, flexGrow: 1 }}>
              <Metric label="RPM" value={rpm} />
              <Metric label="Hız" value={speed} unit={unitFor(m?.machineType)} />
              <Metric label="Yük" value={`${Math.round(load)}%`} />
              <Metric label="Verim" value={`${efficiency}%`} />
              <Metric label="Cycle Time" value={cycle} unit="sn" />
              <Metric label="Titreşim" value={vibration} unit="mm/s" color={vibration > 6 ? '#ef5350' : vibration > 4 ? '#ffa726' : undefined} />
              <Metric label="Sıcaklık" value={temperature} unit="°C" color={temperature > 85 ? '#ef5350' : temperature > 75 ? '#ffa726' : undefined} />
              <Metric label="OEE" value={`${m?.oee ?? 0}%`} color={oeeColor(m?.oee ?? 0)} />
            </Box>
          </Stack>
          <Box sx={{ mt: 2 }}>
            <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
              <Typography variant="caption" color="text.secondary">
                Aşınma (Wear)
              </Typography>
              <Typography variant="caption" sx={{ color: wear > 60 ? '#ef5350' : wear > 40 ? '#ffa726' : '#42a5f5' }}>
                {Math.round(wear)}%
              </Typography>
            </Stack>
            <LinearProgress
              variant="determinate"
              value={Math.min(100, wear)}
              sx={{ height: 8, borderRadius: 4, '& .MuiLinearProgress-bar': { backgroundColor: wear > 60 ? '#ef5350' : wear > 40 ? '#ffa726' : '#42a5f5' } }}
            />
          </Box>
        </CardContent>
      </Card>

      {/* Full-width time-series trends */}
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' } }}>
        <TrendChart title="Sağlık Skoru" data={points} dataKey="health" color="#66bb6a" domain={[0, 100]} area />
        <TrendChart title="Aşınma" data={points} dataKey="wear" color="#ab47bc" unit="%" domain={[0, 100]} />
        <TrendChart title="Titreşim" data={points} dataKey="vibration" color="#26c6da" unit="mm/s" />
        <TrendChart title="Sıcaklık" data={points} dataKey="temperature" color="#ffa726" unit="°C" />
      </Box>
    </Box>
  )
}
