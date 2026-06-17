import { useEffect, useMemo, useState } from 'react'
import type {
  ApiError,
  ProjectDto,
  TaskDto,
  UserProfileDto,
} from '../api/types'
import { api } from '../api/client'

const statusLabels = ['Do zrobienia', 'W trakcie', 'Ukończone', 'Zablokowane']
const priorityLabels = ['Niski', 'Średni', 'Wysoki', 'Krytyczny']

const startOfToday = () => {
  const date = new Date()
  date.setHours(0, 0, 0, 0)
  return date
}

interface ReportsPageProps {
  selectedProjectId: string
  selectedProject: ProjectDto | null
}

const ReportsPage = ({ selectedProjectId, selectedProject }: ReportsPageProps) => {
  const [projects, setProjects] = useState<ProjectDto[]>([])
  const [tasks, setTasks] = useState<TaskDto[]>([])
  const [users, setUsers] = useState<UserProfileDto[]>([])
  const [error, setError] = useState<string | null>(null)

  const stats = useMemo(() => {
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
    const unassignedTasks = tasks.filter((task) => !task.assigneeId).length
    const criticalTasks = tasks.filter((task) => task.priority === 3 && task.status !== 2).length

    const statusCounts = statusLabels.map((_, status) =>
      tasks.filter((task) => task.status === status).length,
    )
    const priorityCounts = priorityLabels.map((_, priority) =>
      tasks.filter((task) => task.priority === priority).length,
    )

    const projectProgress = projects.map((project) => {
      const projectTasks = tasks.filter((task) => task.projectId === project.id)
      const total = projectTasks.length
      const completed = projectTasks.filter((task) => task.status === 2).length
      const blocked = projectTasks.filter((task) => task.status === 3).length
      const overdue = projectTasks.filter((task) => {
        if (!task.dueDate || task.status === 2) {
          return false
        }

        return new Date(task.dueDate) < today
      }).length

      return {
        projectId: project.id,
        name: project.name,
        total,
        completed,
        blocked,
        overdue,
        progress: total ? Math.round((completed / total) * 100) : 0,
      }
    })

    const workload = users
      .map((user) => {
        const assignedTasks = tasks.filter((task) => task.assigneeId === user.userId)
        const active = assignedTasks.filter((task) => task.status !== 2).length
        const completed = assignedTasks.filter((task) => task.status === 2).length
        const critical = assignedTasks.filter((task) => task.priority === 3 && task.status !== 2).length

        return {
          userId: user.userId,
          name: user.displayName,
          active,
          completed,
          critical,
          total: assignedTasks.length,
        }
      })
      .filter((item) => item.total > 0)
      .sort((left, right) => right.active - left.active)

    const completionPercent = tasks.length ? Math.round((completedTasks / tasks.length) * 100) : 0

    return {
      totalProjects: projects.length,
      totalTasks: tasks.length,
      completedTasks,
      activeTasks,
      blockedTasks,
      overdueTasks,
      dueSoonTasks,
      unassignedTasks,
      criticalTasks,
      completionPercent,
      statusCounts,
      priorityCounts,
      projectProgress,
      workload,
    }
  }, [projects, tasks, users])

  const progressSummary = useMemo(() => {
    if (stats.totalTasks === 0) {
      return 'Brak zadań do analizy. Dodaj zadania, aby wygenerować podsumowanie postępu prac.'
    }

    const signals = [
      `Ukończono ${stats.completedTasks} z ${stats.totalTasks} zadań (${stats.completionPercent}%).`,
      stats.activeTasks > 0
        ? `W realizacji pozostaje ${stats.activeTasks} zadań.`
        : 'Nie ma aktywnych zadań do wykonania.',
      stats.overdueTasks > 0
        ? `${stats.overdueTasks} zadań jest po terminie.`
        : 'Brak zadań po terminie.',
      stats.blockedTasks > 0
        ? `${stats.blockedTasks} zadań jest zablokowanych.`
        : 'Brak zadań zablokowanych.',
    ]

    if (stats.criticalTasks > 0) {
      signals.push(`${stats.criticalTasks} aktywnych zadań ma priorytet krytyczny.`)
    }

    return signals.join(' ')
  }, [stats])

  const loadData = () =>
    Promise.all([
      api.getProjects(),
      api.getTasks(selectedProjectId ? { projectId: selectedProjectId } : undefined),
      api.getUsers(),
    ])
      .then(([projectData, taskData, userData]) => {
        const accessibleProjectIds = new Set(projectData.map((project) => project.id))
        const visibleProjects = selectedProject
          ? projectData.filter((project) => project.id === selectedProject.id)
          : projectData

        setProjects(visibleProjects)
        setTasks(taskData.filter((task) => accessibleProjectIds.has(task.projectId)))
        setUsers(userData)
        setError(null)
      })
      .catch((err: unknown) => {
        const status = (err as ApiError)?.status
        setError(status === 401 ? 'Zaloguj się, aby pobrać raporty.' : 'Nie udało się pobrać danych raportów.')
      })

  useEffect(() => {
    loadData()
  }, [selectedProjectId, selectedProject])

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Raporty</h1>
          <p>Automatyczne statystyki postępu prac, ryzyk i obciążenia zespołu.</p>
        </div>
      </header>

      <section className="workspace analytics-workspace">
        <div className="card work wide-panel analytics-summary">
          <div className="analytics-summary-main" aria-label={progressSummary}>
            <h2>Podsumowanie postępu prac</h2>
            {stats.totalTasks === 0 ? (
              <p>{progressSummary}</p>
            ) : (
              <div className="summary-metrics">
                <div className="summary-progress">
                  <strong>{stats.completionPercent}%</strong>
                  <span>Ukończono {stats.completedTasks} z {stats.totalTasks}</span>
                </div>
                <div className="summary-signal">
                  <strong>{stats.activeTasks}</strong>
                  <span>W realizacji</span>
                </div>
                <div className={stats.overdueTasks > 0 ? 'summary-signal danger' : 'summary-signal success'}>
                  <strong>{stats.overdueTasks}</strong>
                  <span>Po terminie</span>
                </div>
                <div className={stats.blockedTasks > 0 ? 'summary-signal warning' : 'summary-signal success'}>
                  <strong>{stats.blockedTasks}</strong>
                  <span>Zablokowane</span>
                </div>
                <div className={stats.criticalTasks > 0 ? 'summary-signal danger' : 'summary-signal success'}>
                  <strong>{stats.criticalTasks}</strong>
                  <span>Krytyczne</span>
                </div>
              </div>
            )}
          </div>
          <div className="summary-badges">
            <span>
              <small>Najbliższy tydzień</small>
              <strong>{stats.dueSoonTasks}</strong>
            </span>
            <span>
              <small>Bez przypisania</small>
              <strong>{stats.unassignedTasks}</strong>
            </span>
            <span>
              <small>Priorytet krytyczny</small>
              <strong>{stats.criticalTasks}</strong>
            </span>
          </div>
        </div>

        {error && (
          <div className="card work wide-panel">
            <div className="empty-state">{error}</div>
          </div>
        )}

        <div className="card work wide-panel">
          <h2>Statystyki zadań</h2>
          <div className="report-stats">
            <div>
              <span>Projekty</span>
              <strong>{stats.totalProjects}</strong>
            </div>
            <div>
              <span>Zadania</span>
              <strong>{stats.totalTasks}</strong>
            </div>
            <div>
              <span>Ukończone</span>
              <strong>{stats.completedTasks}</strong>
            </div>
            <div>
              <span>Postęp</span>
              <strong>{stats.completionPercent}%</strong>
            </div>
            <div>
              <span>Po terminie</span>
              <strong>{stats.overdueTasks}</strong>
            </div>
            <div>
              <span>Zablokowane</span>
              <strong>{stats.blockedTasks}</strong>
            </div>
          </div>

          <div className="report-charts">
            <div className="chart-card">
              <div className="chart-header">
                <span>Ukończenie zadań</span>
                <strong>{stats.completionPercent}%</strong>
              </div>
              <div className="progress-bar">
                <div style={{ width: `${stats.completionPercent}%` }}></div>
              </div>
              <div className="chart-meta">
                <span>Ukończone: {stats.completedTasks}</span>
                <span>Aktywne: {stats.activeTasks}</span>
              </div>
            </div>

            <div className="chart-card">
              <div className="chart-header">
                <span>Statusy</span>
                <strong>{stats.totalTasks}</strong>
              </div>
              <div className="bar-chart">
                {statusLabels.map((label, index) => {
                  const count = stats.statusCounts[index] ?? 0
                  const width = stats.totalTasks ? Math.round((count / stats.totalTasks) * 100) : 0

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

            <div className="chart-card">
              <div className="chart-header">
                <span>Priorytety</span>
                <strong>{stats.criticalTasks} kryt.</strong>
              </div>
              <div className="bar-chart">
                {priorityLabels.map((label, index) => {
                  const count = stats.priorityCounts[index] ?? 0
                  const width = stats.totalTasks ? Math.round((count / stats.totalTasks) * 100) : 0

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
        </div>

        <div className="card work wide-panel">
          <h2>Postęp projektów</h2>
          <div className="project-progress-grid">
            {stats.projectProgress.map((project) => (
              <article key={project.projectId} className="project-progress-card">
                <div className="chart-header">
                  <span>{project.name}</span>
                  <strong>{project.progress}%</strong>
                </div>
                <div className="progress-bar">
                  <div style={{ width: `${project.progress}%` }}></div>
                </div>
                <div className="chart-meta">
                  <span>{project.completed}/{project.total} ukończone</span>
                  <span>{project.overdue} po terminie</span>
                  <span>{project.blocked} blokady</span>
                </div>
              </article>
            ))}
            {stats.projectProgress.length === 0 && <div className="empty-state">Brak projektów do analizy.</div>}
          </div>
        </div>

        <div className="card work wide-panel">
          <h2>Obciążenie zespołu</h2>
          <div className="workload-grid">
            {stats.workload.map((person) => (
              <article key={person.userId} className="workload-card">
                <div>
                  <strong>{person.name}</strong>
                  <span>{person.active} aktywne · {person.completed} ukończone</span>
                </div>
                <div className={person.critical > 0 ? 'risk-pill danger' : 'risk-pill'}>
                  {person.critical > 0 ? `${person.critical} kryt.` : 'stabilnie'}
                </div>
              </article>
            ))}
            {stats.workload.length === 0 && <div className="empty-state">Brak przypisanych zadań.</div>}
          </div>
        </div>
      </section>
    </>
  )
}

export default ReportsPage
