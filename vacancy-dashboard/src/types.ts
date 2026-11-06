export type Exclusions = {
  vacancyIds?: number[] | null
  titleCategories?: string[] | null
} | null

export type SearchPreset = {
  id: string
  key: string
  label: string
  query: string
  defaultExclusions: Exclusions
  createdAt: string
  updatedAt: string
}

export type ParseJob = {
  id: string
  presetKey: string
  presetLabel: string
  query: string
  exclusions: Exclusions
  status: 'pending' | 'queued' | 'completed' | 'failed' | string
  createdAt: string
  queuedAt: string | null
  completedAt: string | null
  failureReason: string | null
}

export type CreatePreset = {
  key: string
  label: string
  query: string
  defaultExclusions: Exclusions
}

export type UpdatePreset = Omit<CreatePreset, 'key'>
