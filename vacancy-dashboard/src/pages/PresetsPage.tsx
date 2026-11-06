import { useState } from 'react'
import { Link } from 'react-router'
import { api } from '../api'
import { useDashboard } from '../app/DashboardContext'
import { Icon } from '../components/ui/Icon'
import PageHeading from '../components/ui/PageHeading'
import { exclusionSummary } from '../shared/format'
import type { SearchPreset } from '../types'

export default function PresetsPage() {
  const { presets, setPresets, setNotice } = useDashboard()
  const [deleteKey, setDeleteKey] = useState('')

  async function removePreset(preset: SearchPreset) {
    try {
      await api.deletePreset(preset.key)
      setPresets((current) => current.filter((item) => item.key !== preset.key))
      setNotice({ kind: 'success', text: 'Пресет удалён.' })
    } catch (error) {
      setNotice({ kind: 'error', text: error instanceof Error ? error.message : 'Не удалось удалить пресет.' })
    } finally {
      setDeleteKey('')
    }
  }

  return (
    <>
      <PageHeading
        title="Пресеты поиска"
        subtitle="Создавайте и редактируйте поисковые запросы HH и исключения к ним."
        action={<Link className="icon-button add-button" to="/presets/new" aria-label="Создать пресет" title="Создать пресет"><Icon name="plus" /></Link>}
      />
      <section className="presets-panel presets-list-panel">
        {presets.length === 0 ? (
          <div className="preset-empty"><span className="empty-mark"><Icon name="layers" size={20} /></span><strong>Пресетов пока нет</strong><span>Создайте пресет с поисковым запросом и исключениями.</span></div>
        ) : (
          <div className="preset-list">
            {presets.map((preset) => (
              <article className="preset-row" key={preset.id}>
                <span className="preset-initial">{preset.label.trim().charAt(0).toLocaleUpperCase('ru-RU')}</span>
                <div className="preset-item-copy">
                  <strong>{preset.label}</strong>
                  <span>{preset.query}</span>
                  <small>{exclusionSummary(preset.defaultExclusions)}</small>
                </div>
                {deleteKey === preset.key ? (
                  <div className="preset-delete-confirm">
                    <span>Удалить пресет?</span>
                    <button className="button danger compact" type="button" onClick={() => void removePreset(preset)}>Удалить</button>
                    <button className="button secondary compact" type="button" onClick={() => setDeleteKey('')}>Отмена</button>
                  </div>
                ) : (
                  <div className="preset-row-actions">
                    <Link className="icon-button" to={`/presets/${encodeURIComponent(preset.key)}/edit`} aria-label={`Изменить пресет ${preset.label}`} title="Изменить"><Icon name="edit" size={16} /></Link>
                    <button className="icon-danger" type="button" onClick={() => setDeleteKey(preset.key)} aria-label={`Удалить пресет ${preset.label}`} title="Удалить"><Icon name="trash" size={16} /></button>
                  </div>
                )}
              </article>
            ))}
          </div>
        )}
      </section>
    </>
  )
}
