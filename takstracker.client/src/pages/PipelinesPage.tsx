import { useEffect, useMemo, useState } from 'react'
import type { ApiError, AuditLogDto } from '../api/types'
import { api } from '../api/client'

const actionLabels: Record<string, string> = {
  'tasks.created': 'Utworzono zadanie',
  'tasks.updated': 'Zaktualizowano zadanie',
  'tasks.deleted': 'Usunięto zadanie',
  'projects.created': 'Utworzono projekt',
  'projects.updated': 'Zaktualizowano projekt',
  'projects.deleted': 'Usunięto projekt',
  'users.created': 'Utworzono użytkownika',
}

const actionTone = (action: string) => {
  if (action.includes('deleted')) {
    return 'danger'
  }
  if (action.includes('updated')) {
    return 'warning'
  }
  return 'success'
}

const formatPayload = (payload: string) => {
  if (!payload) {
    return 'Brak szczegółów'
  }

  try {
    const data = JSON.parse(payload) as Record<string, unknown>
    return Object.entries(data)
      .slice(0, 3)
      .map(([key, value]) => `${key}: ${String(value)}`)
      .join(' · ')
  } catch {
    return payload
  }
}

const PipelinesPage = () => {
  const [logs, setLogs] = useState<AuditLogDto[]>([])
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api
      .getAuditLogs()
      .then((data) => {
        setLogs(data)
        setError(null)
      })
      .catch((err: unknown) => {
        const status = (err as ApiError)?.status
        setError(status === 401 ? 'Zaloguj się, aby pobrać logi.' : 'Nie udało się pobrać logów.')
      })
  }, [])

  const recentLogs = useMemo(() => logs.slice(0, 8), [logs])

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Wdrożenia</h1>
          <p>Historia zmian i zdarzeń systemowych z logów audytu.</p>
        </div>
        <div className="status-pill">{recentLogs.length} zdarzeń</div>
      </header>

      <section className="pipeline-panel">
        <div className="pipeline-header">
          <div>
            <h2>Historia uruchomień</h2>
            <span>Ostatnie operacje z mikroserwisów</span>
          </div>
        </div>

        {error ? (
          <div className="empty-state pipeline-empty">{error}</div>
        ) : (
          <div className="timeline">
            {recentLogs.map((log) => {
              const tone = actionTone(log.action)
              return (
                <article key={log.id} className="timeline-item">
                  <div className={`timeline-marker ${tone}`} aria-hidden="true"></div>
                  <div className="timeline-card">
                    <div className="timeline-card-header">
                      <div>
                        <strong>{actionLabels[log.action] ?? log.action}</strong>
                        <span>{new Date(log.createdAt).toLocaleString('pl-PL')}</span>
                      </div>
                      <span className={`event-pill ${tone}`}>{log.entityType || 'event'}</span>
                    </div>
                    <p>{formatPayload(log.payload)}</p>
                  </div>
                </article>
              )
            })}
            {recentLogs.length === 0 && (
              <div className="empty-state pipeline-empty">
                <strong>Brak danych</strong>
                <span>Uruchom operacje w systemie, aby zobaczyć logi.</span>
              </div>
            )}
          </div>
        )}
      </section>
    </>
  )
}

export default PipelinesPage
