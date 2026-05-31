import { Chip } from '@mui/material'
import { statusColor } from '../theme'

const labels: Record<string, string> = {
  Running: 'Çalışıyor',
  Stopped: 'Durdu',
  Maintenance: 'Bakım',
}

export function StatusChip({ status }: { status: string }) {
  return (
    <Chip
      size="small"
      label={labels[status] ?? status}
      color={statusColor[status] ?? 'default'}
      variant="filled"
      sx={{ fontWeight: 600, minWidth: 92 }}
    />
  )
}
