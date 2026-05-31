import { Badge, IconButton, Tooltip } from '@mui/material'
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useHubEvent } from '../realtime/signalr'

/** AppBar bell showing the live active-alarm count; navigates to the Alarms page. */
export function AlarmBell() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const active = useQuery({ queryKey: ['alarms', 'active'], queryFn: () => api.alarms(true), refetchInterval: 12000 })

  const refresh = () => queryClient.invalidateQueries({ queryKey: ['alarms', 'active'] })
  useHubEvent('AlarmRaised', refresh)
  useHubEvent('AlarmResolved', refresh)

  const count = active.data?.length ?? 0

  return (
    <Tooltip title={count > 0 ? `${count} aktif alarm` : 'Aktif alarm yok'}>
      <IconButton color={count > 0 ? 'error' : 'inherit'} onClick={() => navigate('/alarms')} sx={{ mr: 1 }}>
        <Badge badgeContent={count} color="error">
          <NotificationsActiveIcon />
        </Badge>
      </IconButton>
    </Tooltip>
  )
}
