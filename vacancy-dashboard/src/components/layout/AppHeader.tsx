import { useDashboard } from '../../app/DashboardContext'
import { Icon } from '../ui/Icon'

export default function AppHeader() {
  const { connected, loading, refreshing, refresh } = useDashboard()

  return (
    <header className="topbar">
      <div className="breadcrumb"><strong>hh.ru</strong></div>
      <div className="topbar-right">
        <span className={`connection-state ${connected ? 'connected' : ''}`}><span className="connection-dot" />{connected ? 'API подключён' : loading ? 'Соединяемся' : 'API недоступен'}</span>
        <button className="icon-button quiet" type="button" onClick={() => void refresh(true)} disabled={refreshing} aria-label="Обновить данные" title="Обновить данные">
          <Icon name="refresh" className={refreshing ? 'spin' : ''} />
        </button>
      </div>
    </header>
  )
}
