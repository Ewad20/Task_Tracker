import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { ApiError, NotificationDto, ProjectDto, TaskDto } from '../api/types'
import { api } from '../api/client'
import { connectNotificationStream } from '../api/notificationStream'

const isUnauthorized = (error: unknown) => (error as ApiError)?.status === 401

const formatDate = (value?: string) =>
  value ? new Date(value).toLocaleDateString('pl-PL') : '-'

const statusLabels = ['Do zrobienia', 'W trakcie', 'Ukończone', 'Zablokowane']
const priorityLabels = ['Niski', 'Średni', 'Wysoki', 'Krytyczny']

const startOfToday = () => {
  const date = new Date()
  date.setHours(0, 0, 0, 0)
  return date
}

interface OverviewPageProps {
  selectedProject: ProjectDto | null
  projectCount: number
  accessibleProjectIds: string[]
}

interface MetricItem {
  label: string
  value: string | number
  note?: string
  className?: string
}

const OverviewPage = ({ selectedProject, projectCount, accessibleProjectIds }: OverviewPageProps) => {
  const [tasks, setTasks] = useState<TaskDto[]>([])
  const [notifications, setNotifications] = useState<NotificationDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [now, setNow] = useState(() => new Date())
  const navigate = useNavigate()

  useEffect(() => {
    const loadData = async () =>
      Promise.all([
        api.getTasks(selectedProject ? { projectId: selectedProject.id } : undefined).then((data) =>
          setTasks(
            selectedProject
              ? data
              : data.filter((task) => accessibleProjectIds.includes(task.projectId)),
          ),
        ),
        api.getNotifications(selectedProject?.id).then(setNotifications),
      ])
        .then(() => setError(null))
        .catch((err: unknown) => {
          if (isUnauthorized(err)) {
            setError('Zaloguj się, aby zobaczyć dane z API.')
            return
          }
          setError('Nie udało się pobrać danych z API.')
        })

    loadData()
  }, [accessibleProjectIds, selectedProject])

  useEffect(() => {
    if (!api.getToken()) {
      return undefined
    }

    const selectedProjectId = selectedProject?.id

    return connectNotificationStream((notification) => {
      if (selectedProjectId && notification.projectId !== selectedProjectId) {
        return
      }

      setNotifications((current) => [notification, ...current.filter((item) => item.id !== notification.id)])
    })
  }, [selectedProject?.id])

  useEffect(() => {
    const intervalId = window.setInterval(() => setNow(new Date()), 30_000)
    return () => window.clearInterval(intervalId)
  }, [])

  const taskStats = useMemo(() => {
    const today = startOfToday()
    const weekFromNow = new Date(today)
    weekFromNow.setDate(weekFromNow.getDate() + 7)

    const completedTasks = tasks.filter((task) => task.status === 2).length
    const activeTasks = tasks.filter((task) => task.status !== 2).length
    const blockedTasks = tasks.filter((task) => task.status === 3).length
    const overdueTasks = tasks.filter((task) => {
      if (!task.dueDate || task.status === 2) {
        return false
      }

      return new Date(task.dueDate) < today
    }).length
    const dueSoonTasks = tasks.filter((task) => {
      if (!task.dueDate || task.status === 2) {
        return false
      }

      const dueDate = new Date(task.dueDate)
      return dueDate >= today && dueDate <= weekFromNow
    }).length
    const withoutDueDate = tasks.filter((task) => !task.dueDate && task.status !== 2).length
    const statusCounts = statusLabels.map((_, status) =>
      tasks.filter((task) => task.status === status).length,
    )
    const priorityCounts = priorityLabels.map((_, priority) =>
      tasks.filter((task) => task.priority === priority).length,
    )
    const completionPercent = tasks.length ? Math.round((completedTasks / tasks.length) * 100) : 0

    return {
      activeTasks,
      blockedTasks,
      completedTasks,
      completionPercent,
      dueSoonTasks,
      overdueTasks,
      priorityCounts,
      statusCounts,
      withoutDueDate,
    }
  }, [tasks])

  const todayLabel = useMemo(
    () =>
      now.toLocaleDateString('pl-PL', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
        year: 'numeric',
      }),
    [now],
  )

  const timeLabel = useMemo(
    () =>
      now.toLocaleTimeString('pl-PL', {
        hour: '2-digit',
        minute: '2-digit',
      }),
    [now],
  )

  const metrics: MetricItem[] = [
    {
      label: selectedProject ? 'Wybrany projekt' : 'Projekty',
      value: selectedProject ? selectedProject.name : projectCount,
      note: selectedProject ? 'aktywny filtr danych' : 'łącznie w systemie',
      className: selectedProject ? 'project-context-metric' : undefined,
    },
    { label: 'Zadania', value: tasks.length },
    {
      label: 'Ukończone',
      value: taskStats.completedTasks,
    },
    { label: 'Powiadomienia', value: notifications.length },
  ]

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Strona główna</h1>
          {!selectedProject && (
            <p>{projectCount > 0 ? 'Przegląd wszystkich projektów' : 'Dodaj projekt, aby zobaczyć jego dane'}</p>
          )}
        </div>
        <div className="status-pill">
          {selectedProject ? selectedProject.name : projectCount > 0 ? 'Wszystkie projekty' : 'Brak projektu'}
        </div>
      </header>

      <section className="overview-grid">
        {metrics.map((item) => (
          <div key={item.label} className={`card metric ${item.className ?? ''}`}>
            <span>{item.label}</span>
            <strong>{item.value}</strong>
            <small>{item.note ?? 'Zaktualizowano przed chwilą'}</small>
          </div>
        ))}
        <div className="card wide overview-health-card">
          <h2>Kondycja projektu</h2>
          {error ? (
            <div className="empty-state">{error}</div>
          ) : (
            <div className="report-charts overview-charts">
              <div className="chart-card">
                <div className="chart-header">
                  <span>Ukończenie zadań</span>
                  <strong>{taskStats.completionPercent}%</strong>
                </div>
                <div className="progress-bar">
                  <div style={{ width: `${taskStats.completionPercent}%` }}></div>
                </div>
                <div className="chart-meta">
                  <span>Ukończone: {taskStats.completedTasks}</span>
                  <span>Aktywne: {taskStats.activeTasks}</span>
                </div>
              </div>

              <div className="chart-card">
                <div className="chart-header">
                  <span>Statusy</span>
                  <strong>{tasks.length}</strong>
                </div>
                <div className="bar-chart">
                  {statusLabels.map((label, index) => {
                    const count = taskStats.statusCounts[index] ?? 0
                    const width = tasks.length ? Math.round((count / tasks.length) * 100) : 0

                    return (
                      <div key={label} className="bar-row">
                        <span>{label}</span>
                        <div className="bar">
                          <div style={{ width: `${width}%` }}></div>
                        </div>
                        <strong>{count}</strong>
                      </div>
                    )
                  })}
                </div>
              </div>
            </div>
          )}
        </div>
        <div className="card work overview-today-card">
          <h2>Dzisiaj</h2>
          <div className="today-panel">
            <strong>{timeLabel}</strong>
            <span>{todayLabel}</span>
            <small>{selectedProject ? selectedProject.name : projectCount > 0 ? 'Wszystkie projekty' : 'Brak projektu'}</small>
          </div>
        </div>
        <div className="card work overview-deadline-card">
          <h2>Terminy</h2>
          <div className="deadline-grid">
            <div>
              <span>Po terminie</span>
              <strong>{taskStats.overdueTasks}</strong>
            </div>
            <div>
              <span>Na 7 dni</span>
              <strong>{taskStats.dueSoonTasks}</strong>
            </div>
            <div>
              <span>Bez daty</span>
              <strong>{taskStats.withoutDueDate}</strong>
            </div>
            <div>
              <span>Zablokowane</span>
              <strong>{taskStats.blockedTasks}</strong>
            </div>
          </div>
          <button
            type="button"
            className="btn btn-primary analysis-button"
            onClick={() => navigate('/reports')}
          >
            Otwórz pełną analizę
          </button>
        </div>
      </section>

      <section className="workspace overview-workspace">
        <div className="card work">
          <h2>Zadania projektu</h2>
          <div className="kanban">
            <div>
              <span>Do zrobienia</span>
              <strong>{tasks.filter((task) => task.status === 0).length}</strong>
            </div>
            <div>
              <span>W trakcie</span>
              <strong>{tasks.filter((task) => task.status === 1).length}</strong>
            </div>
            <div>
              <span>Ukończone</span>
              <strong>{tasks.filter((task) => task.status === 2).length}</strong>
            </div>
          </div>
        </div>
        <div className="card work">
          <h2>Powiadomienia</h2>
          <div className="list compact">
            {notifications.slice(0, 3).map((note) => (
              <div key={note.id}>
                <strong>{note.message}</strong>
                <span>{formatDate(note.createdAt)}</span>
              </div>
            ))}
            {notifications.length === 0 && (
              <div>
                <strong>Brak zdarzeń</strong>
                <span>Zdarzenia pojawią się po aktywności w systemie.</span>
              </div>
            )}
          </div>
        </div>
        <div className="card work">
          <h2>Priorytety</h2>
          <div className="bar-chart dashboard-chart">
            {priorityLabels.map((label, index) => {
              const count = taskStats.priorityCounts[index] ?? 0
              const width = tasks.length ? Math.round((count / tasks.length) * 100) : 0

              return (
                <div key={label} className="bar-row">
                  <span>{label}</span>
                  <div className="bar">
                    <div style={{ width: `${width}%` }}></div>
                  </div>
                  <strong>{count}</strong>
                </div>
              )
            })}
          </div>
        </div>
      </section>
    </>
  )
}

export default OverviewPage
