import { useEffect, useMemo, useState } from 'react'
import type { ApiError, ProjectDto, TaskDto, UserProfileDto } from '../api/types'
import { api } from '../api/client'

const statusColumns = [
  { status: 0, label: 'Do zrobienia' },
  { status: 1, label: 'W trakcie' },
  { status: 2, label: 'Ukończone' },
  { status: 3, label: 'Zablokowane' },
]

const priorityLabels = ['Niski', 'Średni', 'Wysoki', 'Krytyczny']

const formatDate = (value?: string | null) =>
  value ? new Intl.DateTimeFormat('pl-PL').format(new Date(value)) : 'Brak terminu'

const isOverdue = (value?: string | null) => {
  if (!value) {
    return false
  }

  const today = new Date()
  today.setHours(0, 0, 0, 0)
  return new Date(value) < today
}

interface MyTasksPageProps {
  selectedProjectId: string
  selectedProject: ProjectDto | null
}

const MyTasksPage = ({ selectedProjectId, selectedProject }: MyTasksPageProps) => {
  const [profile, setProfile] = useState<UserProfileDto | null>(null)
  const [tasks, setTasks] = useState<TaskDto[]>([])
  const [projects, setProjects] = useState<ProjectDto[]>([])
  const [error, setError] = useState<string | null>(null)
  const [updatingTaskId, setUpdatingTaskId] = useState<string | null>(null)

  const loadData = async () => {
    try {
      const currentProfile = await api.getProfile()
      const [taskData, projectData] = await Promise.all([
        api.getTasks({
          assigneeId: currentProfile.userId,
          projectId: selectedProjectId || undefined,
        }),
        api.getProjects(),
      ])
      const accessibleProjectIds = new Set(projectData.map((project) => project.id))

      setProfile(currentProfile)
      setTasks(
        selectedProjectId
          ? taskData
          : taskData.filter((task) => accessibleProjectIds.has(task.projectId)),
      )
      setProjects(projectData)
      setError(null)
    } catch (err: unknown) {
      const status = (err as ApiError)?.status
      setError(status === 401 ? 'Zaloguj się, aby zobaczyć swoje zadania.' : 'Nie udało się pobrać Twoich zadań.')
    }
  }

  useEffect(() => {
    loadData()
  }, [selectedProjectId])

  const stats = useMemo(() => {
    const completed = tasks.filter((task) => task.status === 2).length
    const active = tasks.filter((task) => task.status !== 2).length
    const overdue = tasks.filter((task) => task.status !== 2 && isOverdue(task.dueDate)).length
    const critical = tasks.filter((task) => task.status !== 2 && task.priority === 3).length

    return {
      completed,
      active,
      overdue,
      critical,
      progress: tasks.length ? Math.round((completed / tasks.length) * 100) : 0,
    }
  }, [tasks])

  const getProjectName = (projectId: string) =>
    projects.find((project) => project.id === projectId)?.name ?? projectId

  const updateStatus = async (task: TaskDto, status: number) => {
    if (task.status === status) {
      return
    }

    try {
      setUpdatingTaskId(task.id)
      const updated = await api.updateTask(task.id, {
        title: task.title,
        description: task.description,
        assigneeId: task.assigneeId,
        priority: task.priority,
        status,
        dueDate: task.dueDate,
      })

      setTasks((current) => current.map((item) => (item.id === task.id ? updated : item)))
      setError(null)
    } catch {
      setError('Nie udało się zmienić statusu zadania.')
    } finally {
      setUpdatingTaskId(null)
    }
  }

  return (
    <>
      <header className="page-header">
        <div>
          <h1>Moje zadania</h1>
          <p>
            {profile
              ? `${profile.displayName} · ${selectedProject ? selectedProject.name : 'wszystkie projekty'}`
              : 'Zadania przypisane do Ciebie'}
          </p>
        </div>
      </header>

      <section className="my-tasks-summary">
        <div className="card metric">
          <span>Aktywne</span>
          <strong>{stats.active}</strong>
          <small>zadania do obsłużenia</small>
        </div>
        <div className="card metric">
          <span>Ukończone</span>
          <strong>{stats.completed}</strong>
          <small>zamknięte zadania</small>
        </div>
        <div className="card metric">
          <span>Po terminie</span>
          <strong>{stats.overdue}</strong>
          <small>wymagają uwagi</small>
        </div>
        <div className="card metric">
          <span>Postęp</span>
          <strong>{stats.progress}%</strong>
          <small>Twojej pracy</small>
        </div>
      </section>

      {error && <div className="banner">{error}</div>}

      <section className="my-kanban">
        {statusColumns.map((column) => {
          const columnTasks = tasks.filter((task) => task.status === column.status)

          return (
            <div key={column.status} className="kanban-column">
              <div className="kanban-column-header">
                <h2>{column.label}</h2>
                <span>{columnTasks.length}</span>
              </div>

              <div className="kanban-cards">
                {columnTasks.map((task) => (
                  <article key={task.id} className={`task-card priority-${task.priority}`}>
                    <div className="task-card-header">
                      <strong>{task.title}</strong>
                      <span>{priorityLabels[task.priority] ?? task.priority}</span>
                    </div>
                    <p>{task.description || 'Brak opisu'}</p>
                    <div className="task-card-meta">
                      <span>{getProjectName(task.projectId)}</span>
                      <span className={isOverdue(task.dueDate) && task.status !== 2 ? 'overdue' : ''}>
                        {formatDate(task.dueDate)}
                      </span>
                    </div>
                    <div className="task-card-actions">
                      {statusColumns
                        .filter((item) => item.status !== task.status)
                        .map((item) => (
                          <button
                            key={item.status}
                            type="button"
                            className="btn btn-outline-secondary"
                            onClick={() => updateStatus(task, item.status)}
                            disabled={updatingTaskId === task.id}
                          >
                            {item.label}
                          </button>
                        ))}
                    </div>
                  </article>
                ))}

                {columnTasks.length === 0 && (
                  <div className="kanban-empty">Brak zadań w tej kolumnie.</div>
                )}
              </div>
            </div>
          )
        })}
      </section>
    </>
  )
}

export default MyTasksPage
