import { Navigate, Route, Routes } from 'react-router'
import DashboardLayout from './layouts/DashboardLayout'
import JobsPage from './pages/JobsPage'
import PresetFormPage from './pages/PresetFormPage'
import PresetsPage from './pages/PresetsPage'

export default function App() {
  return (
    <Routes>
      <Route element={<DashboardLayout />}>
        <Route index element={<Navigate to="/presets" replace />} />
        <Route path="presets" element={<PresetsPage />} />
        <Route path="presets/new" element={<PresetFormPage mode="create" />} />
        <Route path="presets/:key/edit" element={<PresetFormPage mode="edit" />} />
        <Route path="jobs" element={<JobsPage />} />
        <Route path="*" element={<Navigate to="/presets" replace />} />
      </Route>
    </Routes>
  )
}
