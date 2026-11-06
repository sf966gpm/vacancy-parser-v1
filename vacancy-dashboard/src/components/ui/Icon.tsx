import type { ReactNode } from 'react'

export type IconName = 'mark' | 'grid' | 'layers' | 'plus' | 'play' | 'edit' | 'trash' | 'refresh' | 'close' | 'check' | 'clock' | 'alert' | 'spinner' | 'arrow' | 'search' | 'briefcase'

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

export function Icon({ name, size = 18, className = '' }: { name: IconName; size?: number; className?: string }) {
  return (
    <svg aria-hidden="true" className={className} width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      {iconPaths[name]}
    </svg>
  )
}
