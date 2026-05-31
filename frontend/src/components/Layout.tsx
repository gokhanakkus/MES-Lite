import {
  AppBar,
  Box,
  Chip,
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
} from '@mui/material'
import DashboardIcon from '@mui/icons-material/Dashboard'
import PrecisionManufacturingIcon from '@mui/icons-material/PrecisionManufacturing'
import MonitorHeartIcon from '@mui/icons-material/MonitorHeart'
import TimelineIcon from '@mui/icons-material/Timeline'
import ReportProblemIcon from '@mui/icons-material/ReportProblem'
import VerifiedIcon from '@mui/icons-material/Verified'
import GroupIcon from '@mui/icons-material/Group'
import AssessmentIcon from '@mui/icons-material/Assessment'
import NotificationsActiveIcon from '@mui/icons-material/NotificationsActive'
import FiberManualRecordIcon from '@mui/icons-material/FiberManualRecord'
import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'
import { useConnectionState } from '../realtime/signalr'
import { AlarmBell } from './AlarmBell'

const drawerWidth = 232

const nav = [
  { to: '/', label: 'Dashboard', icon: <DashboardIcon /> },
  { to: '/machines', label: 'Makineler', icon: <PrecisionManufacturingIcon /> },
  { to: '/health', label: 'Makine Sağlığı', icon: <MonitorHeartIcon /> },
  { to: '/production', label: 'Üretim', icon: <TimelineIcon /> },
  { to: '/downtimes', label: 'Duruşlar', icon: <ReportProblemIcon /> },
  { to: '/quality', label: 'Kalite', icon: <VerifiedIcon /> },
  { to: '/operators', label: 'Operatörler', icon: <GroupIcon /> },
  { to: '/alarms', label: 'Alarmlar', icon: <NotificationsActiveIcon /> },
  { to: '/reports', label: 'Raporlar', icon: <AssessmentIcon /> },
]

export function Layout({ children }: { children: ReactNode }) {
  const connected = useConnectionState()

  return (
    <Box sx={{ display: 'flex' }}>
      <AppBar
        position="fixed"
        elevation={0}
        sx={{
          zIndex: (t) => t.zIndex.drawer + 1,
          bgcolor: 'background.paper',
          borderBottom: '1px solid rgba(255,255,255,0.06)',
        }}
      >
        <Toolbar>
          <PrecisionManufacturingIcon sx={{ mr: 1.5, color: 'primary.main' }} />
          <Typography variant="h6" sx={{ flexGrow: 1, letterSpacing: 0.5 }}>
            MES&nbsp;Lite{' '}
            <Typography component="span" variant="body2" sx={{ color: 'text.secondary' }}>
              · Textile Production & OEE
            </Typography>
          </Typography>
          <AlarmBell />
          <Chip
            size="small"
            icon={<FiberManualRecordIcon sx={{ fontSize: 12 }} />}
            label={connected ? 'Canlı' : 'Bağlanıyor…'}
            color={connected ? 'success' : 'default'}
            variant="outlined"
          />
        </Toolbar>
      </AppBar>

      <Drawer
        variant="permanent"
        sx={{
          width: drawerWidth,
          flexShrink: 0,
          [`& .MuiDrawer-paper`]: {
            width: drawerWidth,
            boxSizing: 'border-box',
            bgcolor: 'background.paper',
            borderRight: '1px solid rgba(255,255,255,0.06)',
          },
        }}
      >
        <Toolbar />
        <List sx={{ px: 1 }}>
          {nav.map((item) => (
            <ListItemButton
              key={item.to}
              component={NavLink}
              to={item.to}
              end={item.to === '/'}
              sx={{
                borderRadius: 2,
                mb: 0.5,
                '&.active': { bgcolor: 'primary.main', color: '#06121f' },
                '&.active .MuiListItemIcon-root': { color: '#06121f' },
              }}
            >
              <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} />
            </ListItemButton>
          ))}
        </List>
      </Drawer>

      <Box component="main" sx={{ flexGrow: 1, p: 3, minHeight: '100vh' }}>
        <Toolbar />
        {children}
      </Box>
    </Box>
  )
}
