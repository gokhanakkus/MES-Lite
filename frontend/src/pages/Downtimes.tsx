import { Box, Card, CardContent, Chip, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { api } from '../api/client'
import { useHubEvent } from '../realtime/signalr'
import { useThrottledInvalidate } from '../hooks/useThrottledInvalidate'

const COLORS = ['#ef5350', '#ffa726', '#42a5f5', '#ab47bc', '#26c6da', '#66bb6a']

const reasonLabels: Record<string, string> = {
  YarnBreak: 'İplik Kopması',
  MaterialWaiting: 'Malzeme Bekleme',
  Maintenance: 'Bakım',
  OperatorWaiting: 'Operatör Bekleme',
  PowerFailure: 'Elektrik Kesintisi',
}

export function Downtimes() {
  const queryClient = useQueryClient()
  const invalidate = useThrottledInvalidate(3000)
  const q = useQuery({ queryKey: ['downtimes', 7], queryFn: () => api.downtimes(7), refetchInterval: 15000 })

  useHubEvent('DowntimeCreated', () => invalidate(() => queryClient.invalidateQueries({ queryKey: ['downtimes'] })))

  const byReason = (q.data?.byReason ?? []).map((r) => ({ name: reasonLabels[r.label] ?? r.label, value: r.count }))
  const byMachine = q.data?.byMachine ?? []

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Duruş Analizi
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Son 7 günün duruş sebepleri ve makine bazlı dağılımı
      </Typography>

      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, mb: 2 }}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Duruş Sebepleri Dağılımı
            </Typography>
            <Box sx={{ height: 320 }}>
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie data={byReason} dataKey="value" nameKey="name" innerRadius={60} outerRadius={110} paddingAngle={2}>
                    {byReason.map((_, i) => (
                      <Cell key={i} fill={COLORS[i % COLORS.length]} />
                    ))}
                  </Pie>
                  <Legend />
                  <Tooltip contentStyle={{ background: '#161d2e', border: '1px solid rgba(255,255,255,0.1)' }} />
                </PieChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Makine Bazlı Toplam Duruş (dk)
            </Typography>
            <Box sx={{ height: 320 }}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={byMachine} margin={{ top: 8, right: 8, bottom: 8, left: -16 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.08)" />
                  <XAxis dataKey="machineName" tick={{ fontSize: 11, fill: '#9aa7bd' }} angle={-25} textAnchor="end" height={60} />
                  <YAxis tick={{ fontSize: 11, fill: '#9aa7bd' }} />
                  <Tooltip
                    contentStyle={{ background: '#161d2e', border: '1px solid rgba(255,255,255,0.1)' }}
                    formatter={(v) => [`${v} dk`, 'Toplam Duruş']}
                  />
                  <Bar dataKey="totalMinutes" fill="#ffa726" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </Box>
          </CardContent>
        </Card>
      </Box>

      <Card>
        <CardContent sx={{ p: 0 }}>
          <Typography variant="h6" sx={{ p: 2, pb: 1 }}>
            Son Duruşlar
          </Typography>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Makine</TableCell>
                <TableCell>Sebep</TableCell>
                <TableCell>Başlangıç</TableCell>
                <TableCell align="right">Süre (dk)</TableCell>
                <TableCell>Durum</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(q.data?.recent ?? []).slice(0, 20).map((d) => (
                <TableRow key={d.id} hover>
                  <TableCell sx={{ fontWeight: 600 }}>{d.machineName}</TableCell>
                  <TableCell>{reasonLabels[d.reason] ?? d.reason}</TableCell>
                  <TableCell sx={{ color: 'text.secondary' }}>
                    {new Date(d.startTime).toLocaleString('tr-TR')}
                  </TableCell>
                  <TableCell align="right">{d.durationMinutes}</TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={d.isOngoing ? 'Devam ediyor' : 'Bitti'}
                      color={d.isOngoing ? 'error' : 'default'}
                      variant={d.isOngoing ? 'filled' : 'outlined'}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </Box>
  )
}
