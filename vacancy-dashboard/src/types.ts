export type Exclusions = {
  vacancyIds?: number[] | null
  titleCategories?: string[] | null
} | null

export type PresetSettings = {
  defaultExclusions?: Exclusions
  [key: string]: unknown
} | null

export type SearchPreset = {
  id: string
  platform: string
  name: string
  query: string
  settings: PresetSettings
  createdAt: string
  updatedAt: string
}

export type ParseJob = {
  id: string
  presetId: string | null
  presetName: string
  platform: string
  query: string
  exclusions: Exclusions
  status: 'pending' | 'queued' | 'completed' | 'failed' | string
  createdAt: string
  queuedAt: string | null
  completedAt: string | null
  failureReason: string | null
}

export type SearchPresetInput = {
  platform: string
  name: string
  query: string
  settings: PresetSettings
}

export type CreatePreset = SearchPresetInput
export type UpdatePreset = SearchPresetInput
