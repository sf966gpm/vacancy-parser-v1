import { Icon, type IconName } from '../../components/ui/Icon'
import { statusText } from './format'

export default function StatusBadge({ status }: { status: string }) {
  const active = status === 'pending' || status === 'queued'
  const icon: IconName = active ? 'clock' : status === 'completed' ? 'check' : 'alert'
  return (
    <span className={`status-badge status-${status}`}>
      <Icon name={icon} size={14} />
      {statusText(status)}
    </span>
  )
}
