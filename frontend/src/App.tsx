import { Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { Dashboard } from './pages/Dashboard'
import { Machines } from './pages/Machines'
import { MachineDetail } from './pages/MachineDetail'
import { MachineHealth } from './pages/MachineHealth'
import { Production } from './pages/Production'
import { Downtimes } from './pages/Downtimes'
import { Quality } from './pages/Quality'
import { Operators } from './pages/Operators'
import { Alarms } from './pages/Alarms'
import { Reports } from './pages/Reports'

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/machines" element={<Machines />} />
        <Route path="/machines/:id" element={<MachineDetail />} />
        <Route path="/health" element={<MachineHealth />} />
        <Route path="/production" element={<Production />} />
        <Route path="/downtimes" element={<Downtimes />} />
        <Route path="/quality" element={<Quality />} />
        <Route path="/operators" element={<Operators />} />
        <Route path="/alarms" element={<Alarms />} />
        <Route path="/reports" element={<Reports />} />
      </Routes>
    </Layout>
  )
}
