import { Card, CardContent, Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'

interface Props {
  label: string
  value: ReactNode
  icon?: ReactNode
  color?: string
  suffix?: string
}

export function KpiCard({ label, value, icon, color = '#42a5f5', suffix }: Props) {
  return (
    <Card sx={{ height: '100%' }}>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Typography variant="body2" color="text.secondary">
            {label}
          </Typography>
          {icon && <Box sx={{ color, display: 'flex' }}>{icon}</Box>}
        </Box>
        <Typography variant="h4" sx={{ mt: 1, color }}>
          {value}
          {suffix && (
            <Typography component="span" variant="h6" sx={{ color: 'text.secondary', ml: 0.5 }}>
              {suffix}
            </Typography>
          )}
        </Typography>
      </CardContent>
    </Card>
  )
}
