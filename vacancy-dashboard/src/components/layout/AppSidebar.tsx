import { Link, NavLink } from 'react-router'
import { useDashboard } from '../../app/DashboardContext'
import { Icon } from '../ui/Icon'

export default function AppSidebar() {
  const { presets, connected } = useDashboard()

  return (
    <aside className="sidebar">
      <Link className="brand" to="/" aria-label="Vacancy Desk — главная">
        <span className="brand-mark"><Icon name="mark" size={19} /></span>
        <span className="brand-name">vacancy<span>desk</span></span>
      </Link>
      <div className="nav-label">Рабочее место</div>
      <nav className="side-nav" aria-label="Основная навигация">
        <NavLink className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`} to="/" end><Icon name="grid" /><span>Обзор</span></NavLink>
        <NavLink className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`} to="/presets"><Icon name="layers" /><span>Пресеты поиска</span><span className="nav-count">{presets.length}</span></NavLink>
        <NavLink className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`} to="/jobs"><Icon name="clock" /><span>Задачи</span></NavLink>
      </nav>
      <div className="sidebar-spacer" />
      <div className="collector-card">
        <div className="collector-orbit"><span /></div>
        <div className="collector-copy"><strong>HH collector</strong><span>Браузерный сборщик</span></div>
        <span className={`collector-dot ${connected ? 'online' : 'offline'}`} title={connected ? 'API доступен' : 'Нет соединения с API'} />
      </div>
      <div className="sidebar-foot">Vacancy Parser <span>·</span> локальная панель</div>
    </aside>
  )
}
