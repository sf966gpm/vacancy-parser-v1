import { Navigate, Route, Routes } from 'react-router'
import DashboardLayout from './layouts/DashboardLayout'
import JobsPage from './pages/JobsPage'
import OverviewPage from './pages/OverviewPage'
import PresetsPage from './pages/PresetsPage'

export default function App() {
  return (
    <Routes>
      <Route element={<DashboardLayout />}>
        <Route index element={<OverviewPage />} />
        <Route path="presets" element={<PresetsPage />} />
        <Route path="jobs" element={<JobsPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}
