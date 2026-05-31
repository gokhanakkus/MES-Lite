import { Box, Card, Chip, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { api } from '../api/client'
import type { AlarmDto } from '../api/types'
import { useHubEvent } from '../realtime/signalr'

const metricLabels: Record<string, string> = {
  Temperature: 'Sıcaklık',
  Vibration: 'Titreşim',
  Rpm: 'Devir',
  Health: 'Sağlık',
  Wear: 'Aşınma',
}
const detectorLabels: Record<string, string> = { Threshold: 'Eşik', Statistical: 'İstatistiksel' }

const sevColor = (s: string) => (s === 'Critical' ? 'error' : 'warning')
const sevLabel = (s: string) => (s === 'Critical' ? 'Kritik' : 'Uyarı')

export function Alarms() {
  const initial = useQuery({ queryKey: ['alarms'], queryFn: () => api.alarms(false, 24) })
  const [rows, setRows] = useState<AlarmDto[]>([])

  useEffect(() => {
    if (initial.data) setRows(initial.data)
  }, [initial.data])

  useHubEvent<AlarmDto>('AlarmRaised', (a) => {
    setRows((prev) => [{ ...a, resolvedAt: null, isActive: true }, ...prev.filter((r) => r.id !== a.id)].slice(0, 200))
  })
  useHubEvent<{ id: number; resolvedAt: string }>('AlarmResolved', (e) => {
    setRows((prev) => prev.map((r) => (r.id === e.id ? { ...r, resolvedAt: e.resolvedAt, isActive: false } : r)))
  })

  const activeCount = rows.filter((r) => r.isActive).length

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
        <Typography variant="h4">Alarmlar & Anomaliler</Typography>
        <Chip
          color={activeCount > 0 ? 'error' : 'success'}
          variant={activeCount > 0 ? 'filled' : 'outlined'}
          label={activeCount > 0 ? `${activeCount} aktif alarm` : 'Aktif alarm yok'}
        />
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Eşik tabanlı ve istatistiksel (geçmiş veriye göre) anomali tespiti — SignalR ile gerçek zamanlı
      </Typography>

      <Card>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Durum</TableCell>
              <TableCell>Önem</TableCell>
              <TableCell>Makine</TableCell>
              <TableCell>Metrik</TableCell>
              <TableCell>Tespit</TableCell>
              <TableCell>Mesaj</TableCell>
              <TableCell>Başlangıç</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((a) => (
              <TableRow key={a.id} hover sx={{ opacity: a.isActive ? 1 : 0.55 }}>
                <TableCell>
                  <Chip
                    size="small"
                    label={a.isActive ? 'Aktif' : 'Çözüldü'}
                    color={a.isActive ? 'error' : 'default'}
                    variant={a.isActive ? 'filled' : 'outlined'}
                  />
                </TableCell>
                <TableCell>
                  <Chip size="small" label={sevLabel(a.severity)} color={sevColor(a.severity)} variant="outlined" />
                </TableCell>
                <TableCell sx={{ fontWeight: 600 }}>{a.machineName}</TableCell>
                <TableCell>{metricLabels[a.metric] ?? a.metric}</TableCell>
                <TableCell>
                  <Chip size="small" variant="outlined" label={detectorLabels[a.detector] ?? a.detector} />
                </TableCell>
                <TableCell sx={{ color: 'text.secondary' }}>{a.message}</TableCell>
                <TableCell sx={{ color: 'text.secondary', whiteSpace: 'nowrap' }}>
                  {new Date(a.raisedAt).toLocaleString('tr-TR')}
                </TableCell>
              </TableRow>
            ))}
            {rows.length === 0 && (
              <TableRow>
                <TableCell colSpan={7} align="center" sx={{ color: 'text.secondary', py: 4 }}>
                  Son 24 saatte alarm yok.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </Card>
    </Box>
  )
}
