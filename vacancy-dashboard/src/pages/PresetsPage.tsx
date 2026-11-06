import { useEffect, useState } from 'react'
import { api } from '../api'
import { useDashboard } from '../app/DashboardContext'
import { Icon } from '../components/ui/Icon'
import PageHeading from '../components/ui/PageHeading'
import PresetEditor from '../features/hh-presets/PresetEditor'
import { exclusionSummary } from '../shared/format'
import type { SearchPreset, UpdatePreset } from '../types'

type EditorState = { mode: 'create' } | { mode: 'edit'; preset: SearchPreset } | null

export default function PresetsPage() {
  const { presets, setPresets, setJobs, connected, setNotice } = useDashboard()
  const [selectedKey, setSelectedKey] = useState('')
  const [editor, setEditor] = useState<EditorState>(null)
  const [saving, setSaving] = useState(false)
  const [running, setRunning] = useState(false)
  const [deleteKey, setDeleteKey] = useState('')

  useEffect(() => {
    if (!presets.some((preset) => preset.key === selectedKey)) setSelectedKey(presets[0]?.key ?? '')
  }, [presets, selectedKey])

  const selectedPreset = presets.find((preset) => preset.key === selectedKey) ?? null

  async function savePreset(key: string, payload: UpdatePreset, isNew: boolean) {
    setSaving(true)
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
      setSaving(false)
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
    setRunning(true)
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
      setRunning(false)
    }
  }

  return (
    <>
      <PageHeading title="Пресеты поиска" subtitle="Настраивайте запросы HH и запускайте сбор вакансий." />
      <div className="workspace-grid workspace-presets">
        <section className="presets-panel">
          <div className="panel-heading preset-panel-heading">
            <div><span className="section-kicker">Настройки сбора</span><h2>Пресеты поиска <span className="total-pill">{presets.length}</span></h2></div>
            <button className="icon-button add-button" type="button" onClick={() => setEditor({ mode: 'create' })} aria-label="Создать пресет"><Icon name="plus" /></button>
          </div>
          {editor ? (
            <PresetEditor key={editor.mode === 'edit' ? editor.preset.id : 'new'} editor={editor} busy={saving} onCancel={() => setEditor(null)} onSave={savePreset} />
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
                  <button className="button run-button" type="button" onClick={() => void runJob()} disabled={running || !connected}>
                    {running ? <Icon name="spinner" className="spin" /> : <Icon name="play" size={17} />}
                    {running ? 'Отправляем задачу…' : 'Запустить поиск'}
                  </button>
                  {!connected && <span className="run-hint">Запуск станет доступен после подключения к API.</span>}
                </div>
              )}
              <button className="add-preset-link" type="button" onClick={() => setEditor({ mode: 'create' })}><Icon name="plus" size={16} />Добавить пресет</button>
            </>
          )}
        </section>
      </div>
    </>
  )
}
