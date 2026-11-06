import type { Notice } from '../../app/DashboardContext'
import { Icon } from '../ui/Icon'

export default function NoticeBanner({ notice, onDismiss }: { notice: Notice; onDismiss: () => void }) {
  return (
    <div className={`notice notice-${notice.kind}`} role="status">
      <Icon name={notice.kind === 'success' ? 'check' : 'alert'} />
      <span>{notice.text}</span>
      <button className="icon-button quiet" type="button" onClick={onDismiss} aria-label="Скрыть уведомление"><Icon name="close" size={16} /></button>
    </div>
  )
}
