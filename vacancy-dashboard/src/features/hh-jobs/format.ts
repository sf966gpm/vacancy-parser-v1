export { dateTime, exclusionSummary } from '../../shared/format'

export function statusText(status: string) {
  switch (status) {
    case 'pending': return 'Ожидает отправки'
    case 'queued': return 'В очереди'
    case 'completed': return 'Завершена'
    case 'failed': return 'Ошибка'
    default: return status
  }
}
