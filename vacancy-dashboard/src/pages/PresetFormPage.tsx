import { useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { api } from '../api'
import { useDashboard } from '../app/DashboardContext'
import { Icon } from '../components/ui/Icon'
import PageHeading from '../components/ui/PageHeading'
import PresetEditor from '../features/presets/PresetEditor'
import type { UpdatePreset } from '../types'

export default function PresetFormPage({ mode }: { mode: 'create' | 'edit' }) {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { presets, loading, setPresets, setNotice } = useDashboard()
  const [saving, setSaving] = useState(false)
  const preset = mode === 'edit' ? presets.find((item) => item.id === id) : undefined

  async function savePreset(payload: UpdatePreset, isNew: boolean) {
    setSaving(true)
    try {
      if (isNew) {
        const created = await api.createPreset(payload)
        setPresets((current) => [...current, created].sort((a, b) => a.platform.localeCompare(b.platform) || a.name.localeCompare(b.name)))
        setNotice({ kind: 'success', text: 'Пресет создан.' })
      } else {
        const updated = await api.updatePreset(preset!.id, payload)
        setPresets((current) => current.map((item) => item.id === preset!.id ? updated : item))
        setNotice({ kind: 'success', text: 'Изменения сохранены.' })
      }
      navigate('/presets')
    } catch (error) {
      setNotice({ kind: 'error', text: error instanceof Error ? error.message : 'Не удалось сохранить пресет.' })
    } finally {
      setSaving(false)
    }
  }

  const backAction = (
    <Link className="button secondary preset-back" to="/presets">
      <Icon name="back" size={16} />К пресетам
    </Link>
  )

  if (mode === 'edit' && loading) {
    return <PageHeading title="Загрузка пресета" subtitle="Получаем данные пресета." action={backAction} />
  }

  if (mode === 'edit' && !preset) {
    return <PageHeading title="Пресет не найден" subtitle="Возможно, он был удалён." action={backAction} />
  }

  const editor = mode === 'create' ? { mode: 'create' as const } : { mode: 'edit' as const, preset: preset! }

  return (
    <>
      <PageHeading
        title={mode === 'create' ? 'Новый пресет' : 'Изменить пресет'}
        subtitle={mode === 'create' ? 'Выберите платформу, задайте поисковый запрос и исключения по умолчанию.' : `Настройте запрос «${preset!.name}» и его исключения.`}
        action={backAction}
      />
      <section className="presets-panel preset-form-page">
        <PresetEditor
          key={mode === 'edit' ? preset!.id : 'new-preset'}
          editor={editor}
          busy={saving}
          onCancel={() => navigate('/presets')}
          onSave={savePreset}
        />
      </section>
    </>
  )
}
