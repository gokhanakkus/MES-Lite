import { Box, Stack, Typography } from '@mui/material'
import { Line, LineChart, ResponsiveContainer, YAxis } from 'recharts'

interface Props {
  label: string
  data: number[]
  color: string
  unit?: string
  height?: number
}

/** Minimal axis-less trend line for a single metric, with the latest value shown. */
export function Sparkline({ label, data, color, unit, height = 44 }: Props) {
  const series = data.map((v, i) => ({ i, v }))
  const last = data.length ? data[data.length - 1] : undefined

  return (
    <Box>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline' }}>
        <Typography variant="caption" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="caption" sx={{ color, fontWeight: 700 }}>
          {last !== undefined ? last : '—'}
          {unit && last !== undefined && <span style={{ opacity: 0.6 }}> {unit}</span>}
        </Typography>
      </Stack>
      <Box sx={{ height }}>
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={series} margin={{ top: 4, right: 2, bottom: 0, left: 2 }}>
            <YAxis hide domain={['dataMin', 'dataMax']} />
            <Line type="monotone" dataKey="v" stroke={color} strokeWidth={2} dot={false} isAnimationActive={false} />
          </LineChart>
        </ResponsiveContainer>
      </Box>
    </Box>
  )
}
