import { Link } from 'react-router'
import type { ParseJob } from '../../types'
import { Icon } from '../../components/ui/Icon'
import StatusBadge from './StatusBadge'
import { dateTime, exclusionSummary } from './format'

export default function JobsTable({
  jobs,
  loading,
  emptyTitle = 'Пока нет запусков',
  emptyMessage = 'Запустите поиск по одному из сохранённых пресетов.',
  showPresetLink = true,
}: {
  jobs: ParseJob[]
  loading: boolean
  emptyTitle?: string
  emptyMessage?: string
  showPresetLink?: boolean
}) {
  if (loading) {
    return <div className="empty-state"><Icon name="spinner" className="spin" /><strong>Загружаем задачи</strong><span>Получаем последние запуски из API.</span></div>
  }

  if (jobs.length === 0) {
    return (
      <div className="empty-state">
        <span className="empty-mark"><Icon name="briefcase" size={22} /></span>
        <strong>{emptyTitle}</strong>
        <span>{emptyMessage}</span>
        {showPresetLink && <Link to="/presets" className="text-link">К пресетам <Icon name="arrow" size={14} /></Link>}
      </div>
    )
  }

  return (
    <div className="jobs-table-wrap">
      <table className="jobs-table">
        <thead><tr><th>ЗАДАЧА</th><th>СТАТУС</th><th>СОЗДАНА</th><th>ИСКЛЮЧЕНИЯ</th></tr></thead>
        <tbody>
          {jobs.map((job) => <JobRow key={job.id} job={job} />)}
        </tbody>
      </table>
    </div>
  )
}

function JobRow({ job }: { job: ParseJob }) {
  return (
    <tr>
      <td className="job-cell">
        <div className="job-title"><div><strong>{job.presetName}</strong><span className="job-platform">{job.platform === 'hh' ? 'HH.ru' : job.platform}</span></div></div>
        {job.failureReason && <p className="job-failure"><Icon name="alert" size={14} />{job.failureReason}</p>}
      </td>
      <td><StatusBadge status={job.status} /></td>
      <td><span className="job-time">{dateTime(job.createdAt)}</span></td>
      <td><span className="exclusion-count">{exclusionSummary(job.exclusions)}</span></td>
    </tr>
  )
}
