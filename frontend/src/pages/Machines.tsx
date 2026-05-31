import {
  Box,
  Card,
  LinearProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { StatusChip } from '../components/StatusChip'
import { useHubEvent } from '../realtime/signalr'
import { healthColor, oeeColor } from '../theme'
import { useThrottledInvalidate } from '../hooks/useThrottledInvalidate'

const typeLabels: Record<string, string> = { Weaving: 'Dokuma', Dyeing: 'Boya', Cutting: 'Kesim' }

export function Machines() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const invalidate = useThrottledInvalidate()
  const machines = useQuery({ queryKey: ['machines'], queryFn: api.machines, refetchInterval: 10000 })

  const refresh = () => invalidate(() => queryClient.invalidateQueries({ queryKey: ['machines'] }))
  useHubEvent('MachineStatusChanged', () => queryClient.invalidateQueries({ queryKey: ['machines'] }))
  useHubEvent('ProductionUpdated', refresh)

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Makineler
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Tüm makinelerin anlık durumu, operatörü ve OEE değeri
      </Typography>

      <Card>
        {machines.isFetching && <LinearProgress />}
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Makine</TableCell>
                <TableCell>Tip</TableCell>
                <TableCell>Hat</TableCell>
                <TableCell>Durum</TableCell>
                <TableCell>Operatör</TableCell>
                <TableCell align="right">Anlık Üretim (son 1s)</TableCell>
                <TableCell align="right">Sağlık</TableCell>
                <TableCell align="right">OEE</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(machines.data ?? []).map((m) => (
                <TableRow key={m.id} hover sx={{ cursor: 'pointer' }} onClick={() => navigate(`/machines/${m.id}`)}>
                  <TableCell sx={{ fontWeight: 600 }}>{m.name}</TableCell>
                  <TableCell>{typeLabels[m.machineType] ?? m.machineType}</TableCell>
                  <TableCell>{m.productionLine}</TableCell>
                  <TableCell>
                    <StatusChip status={m.status} />
                  </TableCell>
                  <TableCell>{m.currentOperator ?? '—'}</TableCell>
                  <TableCell align="right">{m.currentHourProduction.toLocaleString('tr-TR')}</TableCell>
                  <TableCell align="right" sx={{ color: healthColor(m.healthScore), fontWeight: 700 }}>
                    {Math.round(m.healthScore)}
                  </TableCell>
                  <TableCell align="right" sx={{ color: oeeColor(m.oee), fontWeight: 700 }}>
                    {m.oee}%
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>
    </Box>
  )
}
