import {
  Box,
  Button,
  Card,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import DownloadIcon from '@mui/icons-material/Download'
import TableViewIcon from '@mui/icons-material/TableView'
import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../api/client'
import type { PeriodType } from '../api/types'
import { oeeColor } from '../theme'

const periodLabels: Record<PeriodType, string> = { Daily: 'Günlük', Weekly: 'Haftalık', Monthly: 'Aylık' }

export function Reports() {
  const [period, setPeriod] = useState<PeriodType>('Weekly')
  const q = useQuery({ queryKey: ['report', period], queryFn: () => api.report(period) })

  return (
    <Box>
      <Stack
        direction="row"
        sx={{ mb: 1, justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 2 }}
      >
        <Typography variant="h4">Raporlar</Typography>
        <Stack direction="row" spacing={1}>
          <Button
            variant="outlined"
            startIcon={<TableViewIcon />}
            href={api.reportDownloadUrl('csv', period)}
            target="_blank"
          >
            CSV
          </Button>
          <Button
            variant="contained"
            startIcon={<DownloadIcon />}
            href={api.reportDownloadUrl('excel', period)}
            target="_blank"
          >
            Excel
          </Button>
        </Stack>
      </Stack>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Günlük / haftalık / aylık üretim ve OEE raporu — CSV ve Excel olarak indirilebilir
      </Typography>

      <ToggleButtonGroup
        exclusive
        size="small"
        value={period}
        onChange={(_, v) => v && setPeriod(v)}
        sx={{ mb: 2 }}
      >
        {(['Daily', 'Weekly', 'Monthly'] as PeriodType[]).map((p) => (
          <ToggleButton key={p} value={p}>
            {periodLabels[p]}
          </ToggleButton>
        ))}
      </ToggleButtonGroup>

      <Card>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Makine</TableCell>
              <TableCell>Tip</TableCell>
              <TableCell align="right">Üretim</TableCell>
              <TableCell align="right">Hata</TableCell>
              <TableCell align="right">Availability</TableCell>
              <TableCell align="right">Performance</TableCell>
              <TableCell align="right">Quality</TableCell>
              <TableCell align="right">OEE</TableCell>
              <TableCell align="right">Duruş (saat)</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(q.data?.rows ?? []).map((r) => (
              <TableRow key={r.machineId} hover>
                <TableCell sx={{ fontWeight: 600 }}>{r.machineName}</TableCell>
                <TableCell>{r.machineType}</TableCell>
                <TableCell align="right">{r.totalProduction.toLocaleString('tr-TR')}</TableCell>
                <TableCell align="right">{r.totalDefects}</TableCell>
                <TableCell align="right">{r.availability}%</TableCell>
                <TableCell align="right">{r.performance}%</TableCell>
                <TableCell align="right">{r.quality}%</TableCell>
                <TableCell align="right" sx={{ color: oeeColor(r.oee), fontWeight: 700 }}>
                  {r.oee}%
                </TableCell>
                <TableCell align="right">{r.downtimeHours}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Card>
    </Box>
  )
}
