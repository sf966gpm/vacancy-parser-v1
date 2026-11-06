import { useState } from 'react'
import { Link } from 'react-router'
import { api } from '../api'
import { useDashboard } from '../app/DashboardContext'
import { Icon } from '../components/ui/Icon'
import PageHeading from '../components/ui/PageHeading'
import type { SearchPreset } from '../types'

export default function PresetsPage() {
  const { presets, setPresets, setNotice } = useDashboard()
  const [deleteId, setDeleteId] = useState('')

  async function removePreset(preset: SearchPreset) {
    try {
      await api.deletePreset(preset.id)
      setPresets((current) => current.filter((item) => item.id !== preset.id))
      setNotice({ kind: 'success', text: 'Пресет удалён.' })
    } catch (error) {
      setNotice({ kind: 'error', text: error instanceof Error ? error.message : 'Не удалось удалить пресет.' })
    } finally {
      setDeleteId('')
    }
  }

  return (
    <>
      <PageHeading
        title="Пресеты поиска"
        subtitle="Сохраняйте поисковые запросы и настройки для разных платформ."
        action={<Link className="icon-button add-button" to="/presets/new" aria-label="Создать пресет" title="Создать пресет"><Icon name="plus" /></Link>}
      />
      <section className="presets-panel presets-list-panel">
        {presets.length === 0 ? (
          <div className="preset-empty"><span className="empty-mark"><Icon name="layers" size={20} /></span><strong>Пресетов пока нет</strong><span>Создайте пресет и выберите платформу для поиска.</span></div>
        ) : (
          <div className="preset-list">
            {presets.map((preset) => (
              <article className="preset-row" key={preset.id}>
                <span className="preset-initial">{preset.name.trim().charAt(0).toLocaleUpperCase('ru-RU')}</span>
                <div className="preset-item-copy">
                  <strong>{preset.name}</strong>
                  <span className="platform-badge">{preset.platform === 'hh' ? 'HH.ru' : preset.platform}</span>
                </div>
                {deleteId === preset.id ? (
                  <div className="preset-delete-confirm">
                    <span>Удалить пресет?</span>
                    <button className="button danger compact" type="button" onClick={() => void removePreset(preset)}>Удалить</button>
                    <button className="button secondary compact" type="button" onClick={() => setDeleteId('')}>Отмена</button>
                  </div>
                ) : (
                  <div className="preset-row-actions">
                    <Link className="icon-button" to={`/presets/${encodeURIComponent(preset.id)}/edit`} aria-label={`Изменить пресет ${preset.name}`} title="Изменить"><Icon name="edit" size={16} /></Link>
                    <button className="icon-danger" type="button" onClick={() => setDeleteId(preset.id)} aria-label={`Удалить пресет ${preset.name}`} title="Удалить"><Icon name="trash" size={16} /></button>
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
