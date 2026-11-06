import { useCallback, useEffect, useMemo, useState } from 'react'
import { Outlet, useLocation } from 'react-router'
import { api } from '../api'
import { DashboardContext, type DashboardContextValue, type Notice } from '../app/DashboardContext'
import AppHeader from '../components/layout/AppHeader'
import AppSidebar from '../components/layout/AppSidebar'
import NoticeBanner from '../components/layout/NoticeBanner'
import { Icon } from '../components/ui/Icon'
import type { ParseJob, SearchPreset } from '../types'

export default function DashboardLayout() {
  const { pathname } = useLocation()
  const [presets, setPresets] = useState<SearchPreset[]>([])
  const [jobs, setJobs] = useState<ParseJob[]>([])
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [connected, setConnected] = useState(false)
  const [loadError, setLoadError] = useState('')
  const [notice, setNotice] = useState<Notice | null>(null)

  const refresh = useCallback(async (showIndicator = false) => {
    if (showIndicator) setRefreshing(true)
    try {
      const [nextPresets, nextJobs] = await Promise.all([api.getPresets(), api.getJobs()])
      setPresets(nextPresets)
      setJobs(nextJobs)
      setConnected(true)
      setLoadError('')
    } catch (error) {
      setLoadError(error instanceof Error ? error.message : 'Не удалось загрузить данные.')
      setConnected(false)
    } finally {
      setLoading(false)
      setRefreshing(false)
    }
  }, [])

  useEffect(() => { void refresh() }, [refresh])
  useEffect(() => { window.scrollTo({ top: 0, left: 0, behavior: 'auto' }) }, [pathname])
  useEffect(() => {
    if (!notice) return

    const timeout = window.setTimeout(() => setNotice(null), notice.kind === 'success' ? 4000 : 6000)
    return () => window.clearTimeout(timeout)
  }, [notice, setNotice])

  const hasActiveJobs = jobs.some((job) => job.status === 'pending' || job.status === 'queued')
  useEffect(() => {
    if (!hasActiveJobs) return
    const interval = window.setInterval(async () => {
      try {
        setJobs(await api.getJobs())
        setConnected(true)
      } catch {
        setConnected(false)
      }
    }, 5000)
    return () => window.clearInterval(interval)
  }, [hasActiveJobs])

  const contextValue = useMemo<DashboardContextValue>(() => ({
    presets,
    setPresets,
    jobs,
    setJobs,
    loading,
    refreshing,
    connected,
    loadError,
    notice,
    setNotice,
    refresh,
  }), [presets, jobs, loading, refreshing, connected, loadError, notice, refresh])

  return (
    <DashboardContext.Provider value={contextValue}>
      <div className="app-shell">
        <AppSidebar />
        <main className="main-content">
          <AppHeader />
          <div className="page-wrap">
            {notice && <NoticeBanner notice={notice} onDismiss={() => setNotice(null)} />}
            {loadError && (
              <div className="load-error" role="alert">
                <Icon name="alert" />
                <div><strong>Не удалось загрузить данные</strong><span>{loadError}</span></div>
                <button className="button secondary" type="button" onClick={() => void refresh(true)}>Повторить</button>
              </div>
            )}
            <Outlet />
          </div>
        </main>
      </div>
    </DashboardContext.Provider>
  )
}
