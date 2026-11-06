import { createContext, useContext } from 'react'
import type { Dispatch, SetStateAction } from 'react'
import type { ParseJob, SearchPreset } from '../types'

export type Notice = { kind: 'success' | 'error'; text: string }

export type DashboardContextValue = {
  presets: SearchPreset[]
  setPresets: Dispatch<SetStateAction<SearchPreset[]>>
  jobs: ParseJob[]
  setJobs: Dispatch<SetStateAction<ParseJob[]>>
  loading: boolean
  refreshing: boolean
  connected: boolean
  loadError: string
  notice: Notice | null
  setNotice: Dispatch<SetStateAction<Notice | null>>
  refresh: (showIndicator?: boolean) => Promise<void>
}

export const DashboardContext = createContext<DashboardContextValue | null>(null)

export function useDashboard() {
  const context = useContext(DashboardContext)
  if (!context) throw new Error('useDashboard must be used inside DashboardLayout')
  return context
}
