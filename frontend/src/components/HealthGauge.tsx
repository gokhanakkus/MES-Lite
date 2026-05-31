import { Box, CircularProgress, Typography } from '@mui/material'
import { healthColor } from '../theme'

/** Radial Machine Health Score gauge (0-100) with the value in the centre. */
export function HealthGauge({ value, size = 96 }: { value: number; size?: number }) {
  const color = healthColor(value)
  return (
    <Box sx={{ position: 'relative', display: 'inline-flex' }}>
      <CircularProgress variant="determinate" value={100} size={size} thickness={4} sx={{ color: 'rgba(255,255,255,0.08)' }} />
      <CircularProgress
        variant="determinate"
        value={value}
        size={size}
        thickness={4}
        sx={{ color, position: 'absolute', left: 0, '& .MuiCircularProgress-circle': { strokeLinecap: 'round' } }}
      />
      <Box
        sx={{
          position: 'absolute',
          inset: 0,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Typography variant="h6" sx={{ color, lineHeight: 1, fontWeight: 700 }}>
          {Math.round(value)}
        </Typography>
        <Typography variant="caption" sx={{ color: 'text.secondary' }}>
          health
        </Typography>
      </Box>
    </Box>
  )
}
