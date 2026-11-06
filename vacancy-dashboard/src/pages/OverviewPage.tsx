import { Link } from 'react-router'
import { useDashboard } from '../app/DashboardContext'
import { Icon } from '../components/ui/Icon'
import PageHeading from '../components/ui/PageHeading'
import JobsTable from '../features/hh-jobs/JobsTable'

export default function OverviewPage() {
  const { jobs, presets, loading } = useDashboard()
  const activeCount = jobs.filter((job) => job.status === 'pending' || job.status === 'queued').length
  const completedToday = jobs.filter((job) => job.status === 'completed' && job.completedAt && new Date(job.completedAt).toDateString() === new Date().toDateString()).length
  const failedCount = jobs.filter((job) => job.status === 'failed').length

  return (
    <>
      <PageHeading title="Рабочая панель" subtitle="Запускайте поиски и следите за работой сборщика HH." />
      <section className="metrics" aria-label="Сводка по задачам">
        <div className="metric metric-active"><span className="metric-icon"><Icon name="clock" /></span><div><span>В работе</span><strong>{activeCount.toString().padStart(2, '0')}</strong></div><small>сейчас</small></div>
        <div className="metric"><span className="metric-icon done"><Icon name="check" /></span><div><span>Завершено сегодня</span><strong>{completedToday.toString().padStart(2, '0')}</strong></div><small>за день</small></div>
        <div className="metric"><span className="metric-icon failed"><Icon name="alert" /></span><div><span>С ошибкой</span><strong>{failedCount.toString().padStart(2, '0')}</strong></div><small>последние 40</small></div>
      </section>
      <div className="workspace-grid">
        <section className="jobs-panel">
          <div className="panel-heading">
            <div><span className="section-kicker">Последние запуски</span><h2>Задачи парсинга <span className="total-pill">{jobs.length}</span></h2></div>
            <Link className="text-link" to="/jobs">Все задачи <Icon name="arrow" size={14} /></Link>
          </div>
          <JobsTable jobs={jobs.slice(0, 5)} loading={loading} />
        </section>
        <section className="presets-panel overview-presets">
          <div className="panel-heading">
            <div><span className="section-kicker">Настройки сбора</span><h2>Поиски HH <span className="total-pill">{presets.length}</span></h2></div>
            <Icon name="layers" className="overview-panel-icon" />
          </div>
          <p className="overview-presets-copy">Сохраняйте поисковые запросы и запускайте сбор вакансий с выбранными исключениями.</p>
          <Link className="button primary overview-presets-link" to="/presets">Открыть пресеты <Icon name="arrow" size={15} /></Link>
        </section>
      </div>
    </>
  )
}
