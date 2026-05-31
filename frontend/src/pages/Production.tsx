import { Box, Card, CardContent, Chip, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import BoltIcon from '@mui/icons-material/Bolt'
import { useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { api } from '../api/client'
import { useHubEvent } from '../realtime/signalr'

interface FeedItem {
  key: string
  machineName: string
  machineType?: string
  quantity: number
  producedAt: string
}

interface ProductionEvent {
  machineId: number
  machineName: string
  machineType: string
  quantity: number
  producedAt: string
}

const typeLabels: Record<string, string> = { Weaving: 'Dokuma', Dyeing: 'Boya', Cutting: 'Kesim' }

export function Production() {
  const [feed, setFeed] = useState<FeedItem[]>([])
  const [count, setCount] = useState(0)

  const initial = useQuery({ queryKey: ['live'], queryFn: () => api.liveProduction(40) })

  useEffect(() => {
    if (initial.data) {
      setFeed(
        initial.data.map((r) => ({
          key: `init-${r.id}`,
          machineName: r.machineName,
          quantity: r.quantity,
          producedAt: r.producedAt,
        })),
      )
    }
  }, [initial.data])

  useHubEvent<ProductionEvent>('ProductionUpdated', (e) => {
    setCount((c) => c + 1)
    setFeed((prev) =>
      [
        {
          key: `${e.machineId}-${e.producedAt}-${Math.random()}`,
          machineName: e.machineName,
          machineType: e.machineType,
          quantity: e.quantity,
          producedAt: e.producedAt,
        },
        ...prev,
      ].slice(0, 60),
    )
  })

  return (
    <Box>
      <Stack direction="row" sx={{ mb: 1, justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h4">Üretim Akışı</Typography>
        <Chip icon={<BoltIcon />} color="secondary" label={`${count} canlı kayıt`} variant="outlined" />
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Sayfa yenilenmeden, SignalR ile gerçek zamanlı güncellenir
      </Typography>

      <Card>
        <CardContent sx={{ p: 0 }}>
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow>
                <TableCell>Zaman</TableCell>
                <TableCell>Makine</TableCell>
                <TableCell>Tip</TableCell>
                <TableCell align="right">Miktar</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {feed.map((f) => (
                <TableRow key={f.key} hover>
                  <TableCell sx={{ color: 'text.secondary' }}>
                    {new Date(f.producedAt).toLocaleTimeString('tr-TR')}
                  </TableCell>
                  <TableCell sx={{ fontWeight: 600 }}>{f.machineName}</TableCell>
                  <TableCell>{f.machineType ? typeLabels[f.machineType] ?? f.machineType : '—'}</TableCell>
                  <TableCell align="right">{f.quantity.toLocaleString('tr-TR')}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </Box>
  )
}
