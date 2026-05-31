import { Box, Card, Chip, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { oeeColor } from '../theme'

const shiftLabels: Record<string, string> = { Morning: 'Sabah', Evening: 'Akşam', Night: 'Gece' }

export function Operators() {
  const q = useQuery({
    queryKey: ['operators', 7],
    queryFn: () => api.operatorPerformance(7),
    refetchInterval: 20000,
  })

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Operatör Performansı
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
        Son 7 günün operatör bazlı üretim, duruş ve OEE değerleri
      </Typography>

      <Card>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Operatör</TableCell>
              <TableCell>Vardiya</TableCell>
              <TableCell align="right">Toplam Üretim</TableCell>
              <TableCell align="right">Toplam Duruş (dk)</TableCell>
              <TableCell align="right">Ortalama OEE</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {(q.data ?? []).map((o) => (
              <TableRow key={o.operatorId} hover>
                <TableCell sx={{ fontWeight: 600 }}>{o.fullName}</TableCell>
                <TableCell>
                  <Chip size="small" label={shiftLabels[o.shift] ?? o.shift} variant="outlined" />
                </TableCell>
                <TableCell align="right">{o.totalProduction.toLocaleString('tr-TR')}</TableCell>
                <TableCell align="right">{o.totalDowntimeMinutes}</TableCell>
                <TableCell align="right" sx={{ color: oeeColor(o.averageOee), fontWeight: 700 }}>
                  {o.averageOee}%
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Card>
    </Box>
  )
}
