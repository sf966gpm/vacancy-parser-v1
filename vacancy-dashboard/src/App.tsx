import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { api } from './api'
import type { Exclusions, ParseJob, SearchPreset, UpdatePreset } from './types'

type IconName = 'mark' | 'grid' | 'layers' | 'plus' | 'play' | 'edit' | 'trash' | 'refresh' | 'close' | 'check' | 'clock' | 'alert' | 'spinner' | 'arrow' | 'search' | 'briefcase'

const iconPaths: Record<IconName, ReactNode> = {
  mark: <><path d="M5 4h6v16H5z" /><path d="M13 4h6v7h-6zM13 13h6v7h-6z" /></>,
  grid: <><rect x="3.5" y="3.5" width="7" height="7" rx="1.4" /><rect x="13.5" y="3.5" width="7" height="7" rx="1.4" /><rect x="3.5" y="13.5" width="7" height="7" rx="1.4" /><rect x="13.5" y="13.5" width="7" height="7" rx="1.4" /></>,
  layers: <><path d="m12 3 9 5-9 5-9-5 9-5Z" /><path d="m3 12 9 5 9-5M3 16l9 5 9-5" /></>,
  plus: <><path d="M12 5v14M5 12h14" /></>,
  play: <path d="m8 5 11 7-11 7V5Z" />,
  edit: <><path d="m15 5 4 4" /><path d="m4 20 4-.8L19 8a2.1 2.1 0 0 0-3-3L5 16l-1 4Z" /></>,
  trash: <><path d="M4 7h16M10 11v5M14 11v5" /><path d="m6 7 1 13h10l1-13M9 7V4h6v3" /></>,
  refresh: <><path d="M20 7v5h-5M4 17v-5h5" /><path d="M5.6 9a7 7 0 0 1 11.7-2L20 12M4 12l2.7 5a7 7 0 0 0 11.7-2" /></>,
  close: <><path d="m6 6 12 12M18 6 6 18" /></>,
  check: <path d="m5 12 4 4L19 6" />,
  clock: <><circle cx="12" cy="12" r="9" /><path d="M12 7v5l3 2" /></>,
  alert: <><path d="M10.3 4.7 2.8 18a2 2 0 0 0 1.8 3h14.8a2 2 0 0 0 1.8-3L13.7 4.7a2 2 0 0 0-3.4 0Z" /><path d="M12 9v4m0 4h.01" /></>,
  spinner: <><path d="M20 12a8 8 0 1 1-2.34-5.66" /></>,
  arrow: <><path d="M7 17 17 7M7 7h10v10" /></>,
  search: <><circle cx="10.8" cy="10.8" r="6.8" /><path d="m16 16 4 4" /></>,
  briefcase: <><rect x="3" y="7" width="18" height="14" rx="2" /><path d="M8 7V5a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M3 12h18m-11 0v2h4v-2" /></>,
}

function Icon({ name, size = 18, className = '' }: { name: IconName; size?: number; className?: string }) {
  return (
    <svg aria-hidden="true" className={className} width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      {iconPaths[name]}
    </svg>
  )
}

type Notice = { kind: 'success' | 'error'; text: string }
type EditorState = { mode: 'create' } | { mode: 'edit'; preset: SearchPreset } | null
type View = 'overview' | 'presets' | 'jobs'

function parseTokens(value: string) {
  return [...new Set(value.split(/[\n,]/).map((token) => token.trim()).filter(Boolean))]
}

function exclusionSummary(exclusions: Exclusions) {
  const pieces = [
    exclusions?.vacancyIds?.length ? `${exclusions.vacancyIds.length} ID` : null,
    exclusions?.titleCategories?.length ? `${exclusions.titleCategories.length} катег.` : null,
  ].filter(Boolean)
  return pieces.length ? pieces.join(' · ') : 'Нет исключений'
}

function dateTime(value: string) {
  return new Intl.DateTimeFormat('ru-RU', { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' }).format(new Date(value))
}

function statusText(status: string) {
  switch (status) {
    case 'pending': return 'Ожидает отправки'
    case 'queued': return 'В очереди'
    case 'completed': return 'Завершена'
    case 'failed': return 'Ошибка'
    default: return status
  }
}

function StatusBadge({ status }: { status: string }) {
  const active = status === 'pending' || status === 'queued'
  const icon: IconName = active ? 'clock' : status === 'completed' ? 'check' : 'alert'
  return (
    <span className={`status-badge status-${status}`}>
      <Icon name={icon} size={14} />
      {statusText(status)}
    </span>
  )
}

function PresetEditor({
  editor,
  busy,
  onCancel,
  onSave,
}: {
  editor: Exclude<EditorState, null>
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
      <div className="form-heading">
        <div>
          <span className="section-kicker">{editing ? 'Изменение пресета' : 'Новый пресет'}</span>
          <h3>{editing ? editing.label : 'Настроить поиск'}</h3>
        </div>
        <button className="icon-button quiet" type="button" onClick={onCancel} aria-label="Закрыть форму"><Icon name="close" /></button>
      </div>
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

function App() {
  const [currentView, setCurrentView] = useState<View>('overview')
  const [presets, setPresets] = useState<SearchPreset[]>([])
  const [jobs, setJobs] = useState<ParseJob[]>([])
  const [selectedKey, setSelectedKey] = useState('')
  const [editor, setEditor] = useState<EditorState>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [savingPreset, setSavingPreset] = useState(false)
  const [runningJob, setRunningJob] = useState(false)
  const [connected, setConnected] = useState(false)
  const [loadError, setLoadError] = useState('')
  const [notice, setNotice] = useState<Notice | null>(null)
  const [filter, setFilter] = useState<'all' | 'active' | 'completed' | 'failed'>('all')
  const [deleteKey, setDeleteKey] = useState('')

  async function loadData(isRefresh = false) {
    if (isRefresh) setRefreshing(true)
    try {
      const [nextPresets, nextJobs] = await Promise.all([api.getPresets(), api.getJobs()])
      setPresets(nextPresets)
      setJobs(nextJobs)
      setSelectedKey((current) => nextPresets.some((preset) => preset.key === current) ? current : nextPresets[0]?.key ?? '')
      setConnected(true)
      setLoadError('')
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Не удалось загрузить данные.'
      setLoadError(message)
      setConnected(false)
    } finally {
      setLoading(false)
      setRefreshing(false)
    }
  }

  useEffect(() => { void loadData() }, [])

  const hasActiveJobs = jobs.some((job) => job.status === 'pending' || job.status === 'queued')
  useEffect(() => {
    if (!hasActiveJobs) return
    const interval = window.setInterval(async () => {
      try {
        setJobs(await api.getJobs())
        setConnected(true)
      } catch {
        setConnected(false)
      }
    }, 5000)
    return () => window.clearInterval(interval)
  }, [hasActiveJobs])

  const selectedPreset = presets.find((preset) => preset.key === selectedKey) ?? null
  const activeCount = jobs.filter((job) => job.status === 'pending' || job.status === 'queued').length
  const completedToday = jobs.filter((job) => job.status === 'completed' && job.completedAt && new Date(job.completedAt).toDateString() === new Date().toDateString()).length
  const failedCount = jobs.filter((job) => job.status === 'failed').length
  const visibleJobs = useMemo(() => jobs.filter((job) => {
    if (filter === 'active') return job.status === 'pending' || job.status === 'queued'
    if (filter === 'completed' || filter === 'failed') return job.status === filter
    return true
  }), [filter, jobs])

  async function savePreset(key: string, payload: UpdatePreset, isNew: boolean) {
    setSavingPreset(true)
    try {
      if (isNew) {
        const created = await api.createPreset({ key, ...payload })
        setPresets((current) => [...current, created].sort((a, b) => a.key.localeCompare(b.key)))
        setSelectedKey(created.key)
        setNotice({ kind: 'success', text: 'Пресет создан.' })
      } else {
        const updated = await api.updatePreset(key, payload)
        setPresets((current) => current.map((preset) => preset.key === key ? updated : preset))
        setNotice({ kind: 'success', text: 'Изменения сохранены.' })
      }
      setEditor(null)
    } catch (error) {
      setNotice({ kind: 'error', text: error instanceof Error ? error.message : 'Не удалось сохранить пресет.' })
    } finally {
      setSavingPreset(false)
    }
  }

  async function removePreset() {
    if (!deleteKey) return
    const key = deleteKey
    try {
      await api.deletePreset(key)
      setPresets((current) => current.filter((preset) => preset.key !== key))
      setSelectedKey((current) => current === key ? presets.find((preset) => preset.key !== key)?.key ?? '' : current)
      setNotice({ kind: 'success', text: 'Пресет удалён.' })
    } catch (error) {
      setNotice({ kind: 'error', text: error instanceof Error ? error.message : 'Не удалось удалить пресет.' })
    } finally {
      setDeleteKey('')
    }
  }

  async function runJob() {
    if (!selectedPreset) return
    setRunningJob(true)
    try {
      const accepted = await api.createJob(selectedPreset.key)
      setNotice({ kind: 'success', text: `Поиск «${selectedPreset.label}» добавлен в очередь.` })
      try {
        setJobs(await api.getJobs())
      } catch {
        setNotice({ kind: 'success', text: `Задача ${accepted.id.slice(0, 8)} принята. Список обновится при следующем подключении к API.` })
      }
    } catch (error) {
      setNotice({ kind: 'error', text: error instanceof Error ? error.message : 'Не удалось запустить поиск.' })
    } finally {
      setRunningJob(false)
    }
  }

  function navigate(view: View) {
    setCurrentView(view)
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' })
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <button className="brand" type="button" onClick={() => navigate('overview')} aria-label="Vacancy Desk — главная">
          <span className="brand-mark"><Icon name="mark" size={19} /></span>
          <span className="brand-name">vacancy<span>desk</span></span>
        </button>
        <div className="nav-label">Рабочее место</div>
        <nav className="side-nav" aria-label="Основная навигация">
          <button className={`nav-item ${currentView === 'overview' ? 'active' : ''}`} type="button" onClick={() => navigate('overview')} aria-current={currentView === 'overview' ? 'page' : undefined}><Icon name="grid" /><span>Обзор</span></button>
          <button className={`nav-item ${currentView === 'presets' ? 'active' : ''}`} type="button" onClick={() => navigate('presets')} aria-current={currentView === 'presets' ? 'page' : undefined}><Icon name="layers" /><span>Пресеты поиска</span><span className="nav-count">{presets.length}</span></button>
          <button className={`nav-item ${currentView === 'jobs' ? 'active' : ''}`} type="button" onClick={() => navigate('jobs')} aria-current={currentView === 'jobs' ? 'page' : undefined}><Icon name="clock" /><span>Задачи</span></button>
        </nav>
        <div className="sidebar-spacer" />
        <div className="collector-card">
          <div className="collector-orbit"><span /></div>
          <div className="collector-copy"><strong>HH collector</strong><span>Браузерный сборщик</span></div>
          <span className={`collector-dot ${connected ? 'online' : 'offline'}`} title={connected ? 'API доступен' : 'Нет соединения с API'} />
        </div>
        <div className="sidebar-foot">Vacancy Parser <span>·</span> локальная панель</div>
      </aside>

      <main className="main-content" id="overview">
        <header className="topbar">
          <div className="breadcrumb"><span>HH.ru</span><span className="breadcrumb-slash">/</span><strong>{currentView === 'presets' ? 'Пресеты поиска' : currentView === 'jobs' ? 'Задачи' : 'Рабочая панель'}</strong></div>
          <div className="topbar-right">
            <span className={`connection-state ${connected ? 'connected' : ''}`}><span className="connection-dot" />{connected ? 'API подключён' : loading ? 'Соединяемся' : 'API недоступен'}</span>
            <span className="avatar">M</span>
          </div>
        </header>

        <div className="page-wrap">
          <section className="page-heading">
            <div>
              <p className="eyebrow">Центр сбора вакансий <span className="eyebrow-line" /></p>
              <h1>{currentView === 'presets' ? 'Пресеты поиска' : currentView === 'jobs' ? 'Задачи парсинга' : 'Рабочая панель'}</h1>
              <p className="page-subtitle">{currentView === 'presets' ? 'Настраивайте запросы HH и запускайте сбор вакансий.' : currentView === 'jobs' ? 'Следите за очередью и результатом обработки задач.' : 'Запускайте поиски и следите за работой сборщика HH.'}</p>
            </div>
            <button className="button secondary refresh-button" type="button" onClick={() => void loadData(true)} disabled={refreshing}>
              <Icon name="refresh" className={refreshing ? 'spin' : ''} />Обновить
            </button>
          </section>

          {notice && (
            <div className={`notice notice-${notice.kind}`} role="status">
              <Icon name={notice.kind === 'success' ? 'check' : 'alert'} />
              <span>{notice.text}</span>
              <button className="icon-button quiet" type="button" onClick={() => setNotice(null)} aria-label="Скрыть уведомление"><Icon name="close" size={16} /></button>
            </div>
          )}

          {loadError && (
            <div className="load-error" role="alert">
              <Icon name="alert" />
              <div><strong>Не удалось загрузить данные</strong><span>{loadError}</span></div>
              <button className="button secondary" type="button" onClick={() => void loadData(true)}>Повторить</button>
            </div>
          )}

          {currentView !== 'presets' && <section className="metrics" aria-label="Сводка по задачам">
            <div className="metric metric-active"><span className="metric-icon"><Icon name="clock" /></span><div><span>В работе</span><strong>{activeCount.toString().padStart(2, '0')}</strong></div><small>сейчас</small></div>
            <div className="metric"><span className="metric-icon done"><Icon name="check" /></span><div><span>Завершено сегодня</span><strong>{completedToday.toString().padStart(2, '0')}</strong></div><small>за день</small></div>
            <div className="metric"><span className="metric-icon failed"><Icon name="alert" /></span><div><span>С ошибкой</span><strong>{failedCount.toString().padStart(2, '0')}</strong></div><small>последние 40</small></div>
          </section>}

          <div className={`workspace-grid workspace-${currentView}`}>
            {currentView !== 'presets' && <section className="jobs-panel" id="jobs">
              <div className="panel-heading jobs-heading">
                <div><span className="section-kicker">Последние запуски</span><h2>Задачи парсинга <span className="total-pill">{jobs.length}</span></h2></div>
                <div className="job-tools">
                  <div className="filter-tabs" role="group" aria-label="Фильтр задач">
                    {([['all', 'Все'], ['active', 'В работе'], ['completed', 'Готово'], ['failed', 'Ошибки']] as const).map(([value, label]) => (
                      <button key={value} type="button" className={filter === value ? 'selected' : ''} onClick={() => setFilter(value)}>{label}</button>
                    ))}
                  </div>
                </div>
              </div>

              {loading ? (
                <div className="empty-state"><Icon name="spinner" className="spin" /><strong>Загружаем задачи</strong><span>Получаем последние запуски из API.</span></div>
              ) : visibleJobs.length === 0 ? (
                <div className="empty-state">
                  <span className="empty-mark"><Icon name={filter === 'all' ? 'briefcase' : 'search'} size={22} /></span>
                  <strong>{filter === 'all' ? 'Пока нет запусков' : 'В этой группе задач нет'}</strong>
                  <span>{filter === 'all' ? 'Выберите пресет справа и запустите первый поиск.' : 'Попробуйте выбрать другой фильтр.'}</span>
                  {filter === 'all' && <a href="#presets" className="text-link">К пресетам <Icon name="arrow" size={14} /></a>}
                </div>
              ) : (
                <div className="jobs-table-wrap">
                  <table className="jobs-table">
                    <thead><tr><th>ЗАДАЧА</th><th>СТАТУС</th><th>СОЗДАНА</th><th>ИСКЛЮЧЕНИЯ</th></tr></thead>
                    <tbody>
                      {visibleJobs.map((job) => <JobRow key={job.id} job={job} />)}
                    </tbody>
                  </table>
                </div>
              )}
              {jobs.length > 0 && <div className="panel-foot"><span>Обновление каждые 5 секунд, пока есть активные задачи</span><span>Последние {jobs.length} из API</span></div>}
            </section>}

            {currentView !== 'jobs' && <section className="presets-panel" id="presets">
              <div className="panel-heading preset-panel-heading">
                <div><span className="section-kicker">Настройки сбора</span><h2>Пресеты поиска <span className="total-pill">{presets.length}</span></h2></div>
                <button className="icon-button add-button" type="button" onClick={() => setEditor({ mode: 'create' })} aria-label="Создать пресет"><Icon name="plus" /></button>
              </div>
              {editor ? (
                <PresetEditor key={editor.mode === 'edit' ? editor.preset.id : 'new'} editor={editor} busy={savingPreset} onCancel={() => setEditor(null)} onSave={savePreset} />
              ) : presets.length === 0 ? (
                <div className="preset-empty"><span className="empty-mark"><Icon name="layers" size={20} /></span><strong>Создайте первый пресет</strong><span>Сохраните запрос HH, чтобы запускать его в один клик.</span><button className="button primary" type="button" onClick={() => setEditor({ mode: 'create' })}><Icon name="plus" />Новый пресет</button></div>
              ) : (
                <>
                  <div className="preset-list">
                    {presets.map((preset) => (
                      <button key={preset.id} type="button" className={`preset-item ${selectedKey === preset.key ? 'selected' : ''}`} onClick={() => { setSelectedKey(preset.key); setDeleteKey('') }}>
                        <span className="preset-initial">{preset.label.trim().charAt(0).toLocaleUpperCase('ru-RU')}</span>
                        <span className="preset-item-copy"><strong>{preset.label}</strong><span>{preset.query}</span></span>
                        <span className="preset-chev">›</span>
                      </button>
                    ))}
                  </div>
                  {selectedPreset && (
                    <div className="preset-detail">
                      <div className="detail-kicker"><span>Выбранный поиск</span><code>{selectedPreset.key}</code></div>
                      <h3>{selectedPreset.label}</h3>
                      <p className="query-preview"><Icon name="search" size={15} />{selectedPreset.query}</p>
                      <div className="exclusion-summary"><span>Исключения по умолчанию</span><strong>{exclusionSummary(selectedPreset.defaultExclusions)}</strong></div>
                      {deleteKey === selectedPreset.key ? (
                        <div className="delete-confirm"><span>Удалить пресет «{selectedPreset.label}»?</span><button className="button danger" type="button" onClick={() => void removePreset()}>Удалить</button><button className="button secondary compact" type="button" onClick={() => setDeleteKey('')}>Отмена</button></div>
                      ) : (
                        <div className="preset-actions">
                          <button className="button secondary edit-action" type="button" onClick={() => setEditor({ mode: 'edit', preset: selectedPreset })}><Icon name="edit" />Изменить</button>
                          <button className="button icon-danger" type="button" onClick={() => setDeleteKey(selectedPreset.key)} aria-label="Удалить пресет"><Icon name="trash" /></button>
                        </div>
                      )}
                      <button className="button run-button" type="button" onClick={() => void runJob()} disabled={runningJob || !connected}>
                        {runningJob ? <Icon name="spinner" className="spin" /> : <Icon name="play" size={17} />}
                        {runningJob ? 'Отправляем задачу…' : 'Запустить поиск'}
                      </button>
                      {!connected && <span className="run-hint">Запуск станет доступен после подключения к API.</span>}
                    </div>
                  )}
                  <button className="add-preset-link" type="button" onClick={() => setEditor({ mode: 'create' })}><Icon name="plus" size={16} />Добавить пресет</button>
                </>
              )}
            </section>}
          </div>
          <footer className="page-footer"><span>Vacancy Desk</span><span>Сбор вакансий HH.ru</span></footer>
        </div>
      </main>
    </div>
  )
}

function JobRow({ job }: { job: ParseJob }) {
  return (
    <tr>
      <td className="job-cell">
        <div className="job-title"><span className="job-platform">hh</span><div><strong>{job.presetLabel}</strong><span>{job.query}</span></div></div>
        {job.failureReason && <p className="job-failure"><Icon name="alert" size={14} />{job.failureReason}</p>}
      </td>
      <td><StatusBadge status={job.status} /></td>
      <td><span className="job-time">{dateTime(job.createdAt)}</span></td>
      <td><span className="exclusion-count">{exclusionSummary(job.exclusions)}</span></td>
    </tr>
  )
}

export default App
