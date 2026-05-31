import { Box, Card, CardContent, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import InventoryIcon from '@mui/icons-material/Inventory'
import ErrorOutlineIcon from '@mui/icons-material/ReportProblem'
import VerifiedIcon from '@mui/icons-material/Verified'
import { useQuery } from '@tanstack/react-query'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { api } from '../api/client'
import { KpiCard } from '../components/KpiCard'
import { oeeColor } from '../theme'

export function Quality() {
  const q = useQuery({ queryKey: ['quality', 7], queryFn: () => api.quality(7), refetchInterval: 15000 })
  const data = q.data

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Kalite
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Son 7 günün üretim kalitesi ve hata analizi
      </Typography>

      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' }, mb: 3 }}>
        <KpiCard
          label="Toplam Üretim"
          value={data ? data.totalProduced.toLocaleString('tr-TR') : '—'}
          icon={<InventoryIcon />}
        />
        <KpiCard
          label="Toplam Hatalı Üretim"
          value={data ? data.totalDefects.toLocaleString('tr-TR') : '—'}
          color="#ef5350"
          icon={<ErrorOutlineIcon />}
        />
        <KpiCard
          label="Kalite Yüzdesi"
          value={data?.qualityPercentage ?? '—'}
          suffix="%"
          color={oeeColor(data?.qualityPercentage ?? 0)}
          icon={<VerifiedIcon />}
        />
      </Box>

      <Card sx={{ mb: 2 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            Makine Bazlı Hata Adedi
          </Typography>
          <Box sx={{ height: 320 }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data?.defectsByMachine ?? []} margin={{ top: 8, right: 8, bottom: 8, left: -16 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.08)" />
                <XAxis dataKey="label" tick={{ fontSize: 11, fill: '#9aa7bd' }} angle={-25} textAnchor="end" height={60} />
                <YAxis tick={{ fontSize: 11, fill: '#9aa7bd' }} />
                <Tooltip
                  contentStyle={{ background: '#161d2e', border: '1px solid rgba(255,255,255,0.1)' }}
                  formatter={(v) => [`${v}`, 'Hata']}
                />
                <Bar dataKey="count" fill="#ef5350" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </Box>
        </CardContent>
      </Card>

      <Card>
        <CardContent sx={{ p: 0 }}>
          <Typography variant="h6" sx={{ p: 2, pb: 1 }}>
            Son Kalite Kayıtları
          </Typography>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Makine</TableCell>
                <TableCell align="right">Üretim</TableCell>
                <TableCell align="right">Hata</TableCell>
                <TableCell align="right">Kalite %</TableCell>
                <TableCell>Zaman</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(data?.recent ?? []).slice(0, 20).map((r) => (
                <TableRow key={r.id} hover>
                  <TableCell sx={{ fontWeight: 600 }}>{r.machineName}</TableCell>
                  <TableCell align="right">{r.producedQuantity.toLocaleString('tr-TR')}</TableCell>
                  <TableCell align="right" sx={{ color: '#ef5350' }}>{r.defectQuantity}</TableCell>
                  <TableCell align="right" sx={{ color: oeeColor(r.qualityRate), fontWeight: 600 }}>
                    {r.qualityRate}%
                  </TableCell>
                  <TableCell sx={{ color: 'text.secondary' }}>
                    {new Date(r.createdAt).toLocaleString('tr-TR')}
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
