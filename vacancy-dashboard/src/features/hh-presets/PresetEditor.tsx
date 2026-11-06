import { useState, type FormEvent } from 'react'
import type { SearchPreset, UpdatePreset } from '../../types'
import { Icon } from '../../components/ui/Icon'
import { exclusionSummary } from '../../shared/format'

type EditorState = { mode: 'create' } | { mode: 'edit'; preset: SearchPreset }

function parseTokens(value: string) {
  return [...new Set(value.split(/[\n,]/).map((token) => token.trim()).filter(Boolean))]
}

export default function PresetEditor({
  editor,
  busy,
  onCancel,
  onSave,
}: {
  editor: EditorState
  busy: boolean
  onCancel: () => void
  onSave: (key: string, payload: UpdatePreset, isNew: boolean) => Promise<void>
}) {
  const editing = editor.mode === 'edit' ? editor.preset : null
  const [key, setKey] = useState(editing?.key ?? '')
  const [label, setLabel] = useState(editing?.label ?? '')
  const [query, setQuery] = useState(editing?.query ?? '')
  const [vacancyIds, setVacancyIds] = useState(editing?.defaultExclusions?.vacancyIds?.join(', ') ?? '')
  const [categories, setCategories] = useState(editing?.defaultExclusions?.titleCategories?.join(', ') ?? '')
  const [formError, setFormError] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const parsedIds = parseTokens(vacancyIds).map(Number)
    if (parsedIds.some((id) => !Number.isSafeInteger(id) || id <= 0)) {
      setFormError('ID вакансий должны быть положительными целыми числами.')
      return
    }

    const titleCategories = parseTokens(categories)
    const defaultExclusions = parsedIds.length || titleCategories.length
      ? { vacancyIds: parsedIds, titleCategories }
      : null
    const nextKey = key.trim().toLowerCase()
    if (!editing && !/^[a-z0-9][a-z0-9_-]*$/.test(nextKey)) {
      setFormError('Ключ может содержать латинские буквы, цифры, дефис и подчёркивание.')
      return
    }
    setFormError('')
    await onSave(nextKey, { label: label.trim(), query: query.trim(), defaultExclusions }, !editing)
  }

  return (
    <form className="preset-form" onSubmit={submit}>
      <label className="field-label">
        Ключ пресета
        <input required maxLength={100} value={key} onChange={(event) => setKey(event.target.value)} disabled={Boolean(editing)} placeholder="backend-engineer" />
        <span className="field-hint">Стабильный идентификатор, например backend-engineer</span>
      </label>
      <label className="field-label">
        Название
        <input required maxLength={200} value={label} onChange={(event) => setLabel(event.target.value)} placeholder="Backend-разработчики" />
      </label>
      <label className="field-label">
        Поисковый запрос
        <textarea required maxLength={500} rows={3} value={query} onChange={(event) => setQuery(event.target.value)} placeholder="C# .NET разработчик" />
      </label>
      <details className="exclusion-details" open={Boolean(editing?.defaultExclusions)}>
        <summary>Исключения по умолчанию <span>{exclusionSummary(editing?.defaultExclusions ?? null)}</span></summary>
        <label className="field-label">
          ID вакансий
          <textarea rows={2} value={vacancyIds} onChange={(event) => setVacancyIds(event.target.value)} placeholder="123456, 789012" />
          <span className="field-hint">Разделяйте значения запятой или новой строкой.</span>
        </label>
        <label className="field-label">
          Категории названий
          <textarea rows={2} value={categories} onChange={(event) => setCategories(event.target.value)} placeholder="стажёр, помощник" />
        </label>
      </details>
      {formError && <p className="form-error" role="alert">{formError}</p>}
      <div className="form-actions">
        <button className="button secondary" type="button" onClick={onCancel}>Отмена</button>
        <button className="button primary" type="submit" disabled={busy}>
          {busy ? <Icon name="spinner" className="spin" /> : <Icon name="check" />}
          {busy ? 'Сохраняем…' : editing ? 'Сохранить' : 'Создать пресет'}
        </button>
      </div>
    </form>
  )
}
