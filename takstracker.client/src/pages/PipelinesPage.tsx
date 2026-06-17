import { type FormEvent, useEffect, useMemo, useState } from 'react'
import type { ApiError, AuditFilterRequest, AuditLogDto } from '../api/types'
import { api } from '../api/client'

const actionLabels: Record<string, string> = {
  'tasks.created': 'Utworzono zadanie',
  'tasks.updated': 'Zaktualizowano zadanie',
  'tasks.deleted': 'Usunieto zadanie',
  'projects.created': 'Utworzono projekt',
  'projects.updated': 'Zaktualizowano projekt',
  'projects.deleted': 'Usunieto projekt',
  'users.created': 'Utworzono uzytkownika',
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
    return 'Brak szczegolow'
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

const emptyFilters: AuditFilterRequest = {
  userId: '',
  action: '',
  entityType: '',
}

const PipelinesPage = () => {
  const [logs, setLogs] = useState<AuditLogDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [filters, setFilters] = useState<AuditFilterRequest>(emptyFilters)

  const loadLogs = (filter: AuditFilterRequest = filters) => {
    setIsLoading(true)
    api
      .getAuditLogs(filter)
      .then((data) => {
        setLogs(data)
        setError(null)
      })
      .catch((err: unknown) => {
        const status = (err as ApiError)?.status
        setError(status === 401 ? 'Zaloguj sie, aby pobrac logi.' : 'Nie udalo sie pobrac logow.')
      })
      .finally(() => setIsLoading(false))
  }

  useEffect(() => {
    loadLogs(emptyFilters)
  }, [])

  const recentLogs = useMemo(() => logs.slice(0, 8), [logs])
  const uniqueActions = useMemo(
    () => Array.from(new Set(logs.map((log) => log.action))).filter(Boolean).sort(),
    [logs],
  )
  const uniqueEntityTypes = useMemo(
    () => Array.from(new Set(logs.map((log) => log.entityType))).filter(Boolean).sort(),
    [logs],
  )

  const handleFilterSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    loadLogs(filters)
  }

  const handleClearFilters = () => {
    setFilters(emptyFilters)
    loadLogs(emptyFilters)
  }

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Wdrożenia</h1>
          <p>Historia zmian i zdarzeń systemowych z logów audytu</p>
        </div>
        <div className="status-pill">{recentLogs.length} zdarzen</div>
      </header>

      <section className="pipeline-panel">
        <div className="pipeline-header">
          <div>
            <h2>Historia uruchomien</h2>
            <span>Ostatnie operacje z mikroserwisów</span>
          </div>
        </div>

        <form className="audit-filters" onSubmit={handleFilterSubmit}>
          <label>
            Użtkownik
            <input
              type="text"
              value={filters.userId ?? ''}
              placeholder="ID uzytkownika"
              onChange={(event) =>
                setFilters((current) => ({
                  ...current,
                  userId: event.target.value,
                }))
              }
            />
          </label>
          <label>
            Akcja
            <input
              type="text"
              value={filters.action ?? ''}
              list="audit-actions"
              placeholder="np. tasks.created"
              onChange={(event) =>
                setFilters((current) => ({
                  ...current,
                  action: event.target.value,
                }))
              }
            />
            <datalist id="audit-actions">
              {uniqueActions.map((action) => (
                <option key={action} value={action} />
              ))}
            </datalist>
          </label>
          <label>
            Typ encji
            <input
              type="text"
              value={filters.entityType ?? ''}
              list="audit-entity-types"
              placeholder="np. Task"
              onChange={(event) =>
                setFilters((current) => ({
                  ...current,
                  entityType: event.target.value,
                }))
              }
            />
            <datalist id="audit-entity-types">
              {uniqueEntityTypes.map((entityType) => (
                <option key={entityType} value={entityType} />
              ))}
            </datalist>
          </label>
          <div className="audit-filter-actions">
            <button type="submit" className="btn btn-primary" disabled={isLoading}>
              Filtruj
            </button>
            <button
              type="button"
              className="btn btn-outline-secondary"
              onClick={handleClearFilters}
              disabled={isLoading}
            >
              Wyczyść
            </button>
          </div>
        </form>

        {isLoading ? (
          <div className="empty-state pipeline-empty">Ladowanie logow...</div>
        ) : error ? (
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
                <span>Zmien filtry albo wykonaj operacje w systemie, aby zobaczyc logi.</span>
              </div>
            )}
          </div>
        )}
      </section>
    </>
  )
}

export default PipelinesPage
