import { Box, Card, CardContent, Chip, Stack, Typography } from '@mui/material'
import PrecisionManufacturingIcon from '@mui/icons-material/PrecisionManufacturing'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import StopIcon from '@mui/icons-material/Stop'
import SpeedIcon from '@mui/icons-material/Speed'
import InventoryIcon from '@mui/icons-material/Inventory'
import PsychologyIcon from '@mui/icons-material/Psychology'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { api } from '../api/client'
import { KpiCard } from '../components/KpiCard'
import { useHubEvent } from '../realtime/signalr'
import { oeeColor } from '../theme'
import { useThrottledInvalidate } from '../hooks/useThrottledInvalidate'

export function Dashboard() {
  const queryClient = useQueryClient()
  const invalidate = useThrottledInvalidate()

  const kpis = useQuery({ queryKey: ['kpis'], queryFn: api.dashboardKpis, refetchInterval: 15000 })
  const oee = useQuery({ queryKey: ['oee', 'Daily'], queryFn: () => api.oeeDashboard('Daily') })
  const insights = useQuery({ queryKey: ['insights', 30], queryFn: () => api.maintenanceInsights(30) })

  // Real-time refresh on simulator events.
  useHubEvent('OeeUpdated', () => invalidate(() => {
    queryClient.invalidateQueries({ queryKey: ['oee'] })
    queryClient.invalidateQueries({ queryKey: ['kpis'] })
  }))
  useHubEvent('MachineStatusChanged', () => queryClient.invalidateQueries({ queryKey: ['kpis'] }))

  const k = kpis.data
  const chartData = (oee.data?.machines ?? []).map((m) => ({ name: m.machineName, oee: m.oee }))

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Genel Bakış
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Fabrika geneli canlı üretim ve OEE durumu
      </Typography>

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(5, 1fr)' },
          mb: 3,
        }}
      >
        <KpiCard label="Toplam Makine" value={k?.totalMachines ?? '—'} icon={<PrecisionManufacturingIcon />} />
        <KpiCard label="Çalışan" value={k?.runningMachines ?? '—'} color="#66bb6a" icon={<PlayArrowIcon />} />
        <KpiCard label="Duran" value={k?.stoppedMachines ?? '—'} color="#ef5350" icon={<StopIcon />} />
        <KpiCard
          label="Bugünkü Üretim"
          value={k ? k.todayProduction.toLocaleString('tr-TR') : '—'}
          color="#26c6da"
          icon={<InventoryIcon />}
        />
        <KpiCard
          label="Ortalama OEE"
          value={k?.averageOee ?? '—'}
          suffix="%"
          color={oeeColor(k?.averageOee ?? 0)}
          icon={<SpeedIcon />}
        />
      </Box>

      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', lg: '2fr 1fr' } }}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Makine Bazlı OEE (Günlük)
            </Typography>
            <Box sx={{ height: 340 }}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={chartData} margin={{ top: 8, right: 8, bottom: 8, left: -16 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.08)" />
                  <XAxis dataKey="name" tick={{ fontSize: 11, fill: '#9aa7bd' }} angle={-25} textAnchor="end" height={60} />
                  <YAxis domain={[0, 100]} tick={{ fontSize: 11, fill: '#9aa7bd' }} />
                  <Tooltip
                    contentStyle={{ background: '#161d2e', border: '1px solid rgba(255,255,255,0.1)' }}
                    formatter={(v) => [`${v}%`, 'OEE']}
                  />
                  <Bar dataKey="oee" radius={[6, 6, 0, 0]}>
                    {chartData.map((d, i) => (
                      <Cell key={i} fill={oeeColor(d.oee)} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Stack direction="row" spacing={1} sx={{ mb: 1, alignItems: 'center' }}>
              <PsychologyIcon sx={{ color: 'secondary.main' }} />
              <Typography variant="h6">AI Bakım Önerileri</Typography>
            </Stack>
            <Stack spacing={1.5} sx={{ maxHeight: 320, overflowY: 'auto' }}>
              {(insights.data ?? []).slice(0, 4).map((i) => (
                <Box key={i.machineId} sx={{ p: 1.5, borderRadius: 2, bgcolor: 'rgba(255,255,255,0.03)' }}>
                  <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="subtitle2">{i.machineName}</Typography>
                    <Chip
                      size="small"
                      label={i.severity}
                      color={i.severity === 'Yüksek' ? 'error' : i.severity === 'Orta' ? 'warning' : 'default'}
                    />
                  </Stack>
                  <Typography variant="caption" color="text.secondary">
                    {i.recommendation}
                  </Typography>
                </Box>
              ))}
              {insights.data?.length === 0 && (
                <Typography variant="body2" color="text.secondary">
                  Henüz öneri yok.
                </Typography>
              )}
            </Stack>
          </CardContent>
        </Card>
      </Box>
    </Box>
  )
}
