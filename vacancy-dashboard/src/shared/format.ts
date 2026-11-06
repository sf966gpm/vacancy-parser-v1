import type { Exclusions } from '../types'

export function exclusionSummary(exclusions: Exclusions) {
  const pieces = [
    exclusions?.vacancyIds?.length ? `${exclusions.vacancyIds.length} ID` : null,
    exclusions?.titleCategories?.length ? `${exclusions.titleCategories.length} катег.` : null,
  ].filter(Boolean)
  return pieces.length ? pieces.join(' · ') : 'Нет исключений'
}

export function dateTime(value: string) {
  return new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
}
