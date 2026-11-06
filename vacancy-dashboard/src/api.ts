import type { CreatePreset, ParseJob, SearchPreset, UpdatePreset } from './types'

type Problem = {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      ...init,
      headers: {
        ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
        ...init?.headers,
      },
    })
  } catch {
    throw new Error('Не удалось связаться с API. Проверьте, запущен ли сервис.')
  }

  if (!response.ok) {
    let problem: Problem = {}
    try {
      problem = await response.json() as Problem
    } catch {
      // The API may return an empty response for errors without a problem body.
    }
    const validation = problem.errors
      ? Object.values(problem.errors).flat().join(' ')
      : undefined
    throw new Error(validation || problem.detail || problem.title || `Ошибка API (${response.status})`)
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export const api = {
  getPresets: () => request<SearchPreset[]>('/presets'),
  createPreset: (preset: CreatePreset) => request<SearchPreset>('/presets', {
    method: 'POST',
    body: JSON.stringify(preset),
  }),
  updatePreset: (id: string, preset: UpdatePreset) => request<SearchPreset>(`/presets/${encodeURIComponent(id)}`, {
    method: 'PUT',
    body: JSON.stringify(preset),
  }),
  deletePreset: (id: string) => request<void>(`/presets/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  }),
  getJobs: () => request<ParseJob[]>('/hh/jobs?take=40'),
}
