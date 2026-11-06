import { useLocation } from 'react-router'
import { useDashboard } from '../../app/DashboardContext'
import { Icon } from '../ui/Icon'

export default function AppHeader() {
  const { connected, loading, refreshing, refresh } = useDashboard()
  const { pathname } = useLocation()
  const sectionName = pathname === '/presets' ? 'Пресеты поиска' : pathname === '/jobs' ? 'Задачи' : 'Рабочая панель'

  return (
    <header className="topbar">
      <div className="breadcrumb"><span>HH.ru</span><span className="breadcrumb-slash">/</span><strong>{sectionName}</strong></div>
      <div className="topbar-right">
        <span className={`connection-state ${connected ? 'connected' : ''}`}><span className="connection-dot" />{connected ? 'API подключён' : loading ? 'Соединяемся' : 'API недоступен'}</span>
        <button className="icon-button quiet" type="button" onClick={() => void refresh(true)} disabled={refreshing} aria-label="Обновить данные" title="Обновить данные">
          <Icon name="refresh" className={refreshing ? 'spin' : ''} />
        </button>
      </div>
    </header>
  )
}
