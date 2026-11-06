import { useMemo, useState } from 'react'
import { useDashboard } from '../app/DashboardContext'
import JobsTable from '../features/hh-jobs/JobsTable'
import PageHeading from '../components/ui/PageHeading'

type JobFilter = 'all' | 'active' | 'completed' | 'failed'

const filters: [JobFilter, string][] = [
  ['all', 'Все'],
  ['active', 'В работе'],
  ['completed', 'Готово'],
  ['failed', 'Ошибки'],
]

export default function JobsPage() {
  const { jobs, loading } = useDashboard()
  const [filter, setFilter] = useState<JobFilter>('all')
  const visibleJobs = useMemo(() => jobs.filter((job) => {
    if (filter === 'active') return job.status === 'pending' || job.status === 'queued'
    if (filter === 'completed' || filter === 'failed') return job.status === filter
    return true
  }), [filter, jobs])

  return (
    <>
      <PageHeading title="Задачи парсинга" subtitle="Следите за очередью и результатом обработки задач." />
      <section className="jobs-panel">
        <div className="panel-heading jobs-heading">
          <div><span className="section-kicker">История запусков</span><h2>Все задачи <span className="total-pill">{jobs.length}</span></h2></div>
          <div className="job-tools">
            <div className="filter-tabs" role="group" aria-label="Фильтр задач">
              {filters.map(([value, label]) => <button key={value} type="button" className={filter === value ? 'selected' : ''} onClick={() => setFilter(value)}>{label}</button>)}
            </div>
          </div>
        </div>
        <JobsTable
          jobs={visibleJobs}
          loading={loading}
          emptyTitle={filter === 'all' ? 'Пока нет запусков' : 'В этой группе задач нет'}
          emptyMessage={filter === 'all' ? 'Запустите поиск по одному из сохранённых пресетов.' : 'Попробуйте выбрать другой фильтр.'}
          showPresetLink={filter === 'all'}
        />
        {jobs.length > 0 && <div className="panel-foot"><span>Обновление каждые 5 секунд, пока есть активные задачи</span><span>Последние {jobs.length} из API</span></div>}
      </section>
    </>
  )
}
