import { useEffect, type ReactNode } from 'react'

export default function PageHeading({ title, subtitle, action }: { title: string; subtitle: string; action?: ReactNode }) {
  useEffect(() => { document.title = `${title} · Vacancy Desk` }, [title])

  return (
    <section className="page-heading">
      <div>
        <h1>{title}</h1>
        <p className="page-subtitle">{subtitle}</p>
      </div>
      {action}
    </section>
  )
}
