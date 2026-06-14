import { useCallback, useEffect, useMemo, useState } from 'react'
import { NavLink, Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import { api } from './api/client'
import type { ProjectDto, UserProfileDto } from './api/types'
import OverviewPage from './pages/OverviewPage'
import BoardsPage from './pages/BoardsPage'
import MyTasksPage from './pages/MyTasksPage'
import PipelinesPage from './pages/PipelinesPage'
import ReposPage from './pages/ReposPage'
import ReportsPage from './pages/ReportsPage'
import SettingsPage from './pages/SettingsPage'
import AuthPage from './pages/AuthPage'

interface NavigationItem {
  label: string
  path: string
  icon: IconName
  requiresAdmin?: boolean
}

const navigation: NavigationItem[] = [
  { label: 'Strona główna', path: '/overview', icon: 'home' },
  { label: 'Wszystkie zadania', path: '/boards', icon: 'board' },
  { label: 'Moje zadania', path: '/my-tasks', icon: 'myTasks' },
  { label: 'Wdrożenia', path: '/pipelines', icon: 'pipeline' },
  { label: 'Projekty', path: '/repos', icon: 'project' },
  { label: 'Raporty', path: '/reports', icon: 'report' },
  { label: 'Ustawienia', path: '/settings', icon: 'settings', requiresAdmin: true },
]

const iconPaths = {
  home: 'M3 10.5 12 3l9 7.5V21h-6v-6H9v6H3V10.5Z',
  board: 'M4 5h16v14H4V5Zm5 0v14m6-14v14',
  myTasks: 'M8 5h11v4H8V5Zm0 6h11v4H8v-4Zm0 6h11v2H8v-2ZM4 6.5l1 1 2-2M4 12.5l1 1 2-2M4 18.5l1 1 2-2',
  pipeline: 'M4 7h6v4H4V7Zm10 6h6v4h-6v-4ZM10 9h2a3 3 0 0 1 3 3v1',
  project: 'M3 6h7l2 3h9v10H3V6Z',
  report: 'M6 3h9l3 3v15H6V3Zm3 8h6M9 15h6',
  settings: 'M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8Zm0-5v3m0 12v3M4.2 4.2l2.1 2.1m11.4 11.4 2.1 2.1M3 12h3m12 0h3M4.2 19.8l2.1-2.1M17.7 6.3l2.1-2.1',
  collapse: 'M15 6 9 12l6 6',
  expand: 'm9 6 6 6-6 6',
}

type IconName = keyof typeof iconPaths
const selectedProjectKey = 'tt_selected_project_id'
const projectsChangedEvent = 'projects-changed'

const AppIcon = ({ name }: { name: IconName }) => (
  <svg className="app-icon" viewBox="0 0 24 24" aria-hidden="true">
    <path d={iconPaths[name]} />
  </svg>
)

function App() {
  const [isNavigationOpen, setIsNavigationOpen] = useState(true)
  const [profile, setProfile] = useState<UserProfileDto | null>(null)
  const [projects, setProjects] = useState<ProjectDto[]>([])
  const [hasToken, setHasToken] = useState(() => Boolean(api.getToken()))
  const [selectedProjectId, setSelectedProjectId] = useState(
    () => localStorage.getItem(selectedProjectKey) ?? '',
  )
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false)
  const initials = useMemo(() => {
    const name = profile?.displayName?.trim()
    if (!name) {
      return 'U'
    }

    const parts = name.split(/\s+/)
    return `${parts[0]?.[0] ?? ''}${parts.length > 1 ? parts[parts.length - 1][0] : ''}`.toUpperCase()
  }, [profile])

  const loadProfile = useCallback(async () => {
    if (!api.getToken()) {
      setProfile(null)
      return
    }

    try {
      const data = await api.getProfile()
      setProfile(data)
    } catch {
      api.setToken(null)
      setProfile(null)
    }
  }, [])

  useEffect(() => {
    loadProfile()
    window.addEventListener('auth-changed', loadProfile)
    return () => window.removeEventListener('auth-changed', loadProfile)
  }, [loadProfile])

  useEffect(() => {
    const updateToken = () => setHasToken(Boolean(api.getToken()))
    updateToken()
    window.addEventListener('auth-changed', updateToken)
    return () => window.removeEventListener('auth-changed', updateToken)
  }, [])

  useEffect(() => {
    const loadProjects = async () => {
      if (!api.getToken()) {
        setProjects([])
        setSelectedProjectId('')
        return
      }

      try {
        const data = await api.getProjects()
        setProjects(data)
        setSelectedProjectId((currentProjectId) => {
          if (currentProjectId && data.some((project) => project.id === currentProjectId)) {
            return currentProjectId
          }

          localStorage.removeItem(selectedProjectKey)
          return ''
        })
      } catch {
        setProjects([])
      }
    }

    loadProjects()
    window.addEventListener('auth-changed', loadProjects)
    window.addEventListener(projectsChangedEvent, loadProjects)
    return () => {
      window.removeEventListener('auth-changed', loadProjects)
      window.removeEventListener(projectsChangedEvent, loadProjects)
    }
  }, [])

  const handleProjectChange = (projectId: string) => {
    setSelectedProjectId(projectId)
    if (projectId) {
      localStorage.setItem(selectedProjectKey, projectId)
    } else {
      localStorage.removeItem(selectedProjectKey)
    }
  }

  const selectedProject = projects.find((project) => project.id === selectedProjectId) ?? null
  const accessibleProjectIds = useMemo(() => projects.map((project) => project.id), [projects])
  const isAdmin = profile?.role === 'Admin'

  const handleLogout = () => {
    api.setToken(null)
    setProfile(null)
    setIsUserMenuOpen(false)
  }

  if (!hasToken) {
    return <AuthPage onAuthSuccess={loadProfile} />
  }

  return (
    <div className="app-shell">
      <header className="topbar">
        <div className="topbar-left">
          <NavLink className="brand" to="/overview" aria-label="Przejdź do strony głównej">
            <span className="brand-mark" aria-hidden="true">
              <span className="brand-check"></span>
            </span>
            <strong>TaskTracker</strong>
          </NavLink>
        </div>
        <label className="project-picker">
          <span>Projekt</span>
          <select
            value={selectedProjectId}
            onChange={(event) => handleProjectChange(event.target.value)}
            disabled={projects.length === 0}
          >
            {projects.length === 0 ? (
              <option value="">Brak projektów</option>
            ) : (
              <>
                <option value="">Wszystkie projekty</option>
                {projects.map((project) => (
                  <option key={project.id} value={project.id}>
                    {project.name}
                  </option>
                ))}
              </>
            )}
          </select>
        </label>
        <div className="topbar-right">
          <div className="user-menu">
            <button
              type="button"
              className="avatar"
              onClick={() => setIsUserMenuOpen((isOpen) => !isOpen)}
              aria-expanded={isUserMenuOpen}
              aria-label="Menu użytkownika"
            >
              {initials}
            </button>
            {isUserMenuOpen && (
              <div className="user-dropdown">
                <div>
                  <strong>{profile?.displayName ?? 'Użytkownik'}</strong>
                  <span>{profile ? 'Aktywna sesja' : 'Brak aktywnej sesji'}</span>
                </div>
                <button type="button" className="btn btn-outline-danger" onClick={handleLogout} disabled={!profile}>
                  Wyloguj
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      <div className="main-area">
        <aside className={`sidebar ${isNavigationOpen ? '' : 'collapsed'}`}>
          <button
            type="button"
            className="menu-toggle"
            onClick={() => setIsNavigationOpen((isOpen) => !isOpen)}
            aria-expanded={isNavigationOpen}
            aria-label={isNavigationOpen ? 'Zwiń menu' : 'Rozwiń menu'}
          >
            <AppIcon name={isNavigationOpen ? 'collapse' : 'expand'} />
          </button>
          {isNavigationOpen && <div className="section-title">Nawigacja</div>}
          <nav className="nav">
            {navigation
              .filter((item) => !item.requiresAdmin || isAdmin)
              .map((item) => (
                <NavLink key={item.label} to={item.path} title={item.label}>
                  <AppIcon name={item.icon} />
                  <span>{item.label}</span>
                </NavLink>
              ))}
          </nav>
          {isNavigationOpen && (
            <div className="api-box">
              <span>Brama API</span>
              <strong>http://localhost:8080</strong>
              <small>Mikroserwisy online</small>
            </div>
          )}
        </aside>

        <main className="content">
          <Routes>
            <Route path="/" element={<Navigate to="/overview" replace />} />
            <Route
              path="/overview"
              element={
                <OverviewPage
                  selectedProject={selectedProject}
                  projectCount={projects.length}
                  accessibleProjectIds={accessibleProjectIds}
                />
              }
            />
            <Route
              path="/boards"
              element={
                <BoardsPage
                  selectedProjectId={selectedProjectId}
                  selectedProject={selectedProject}
                  currentUser={profile}
                />
              }
            />
            <Route
              path="/my-tasks"
              element={<MyTasksPage selectedProjectId={selectedProjectId} selectedProject={selectedProject} />}
            />
            <Route path="/pipelines" element={<PipelinesPage />} />
            <Route path="/repos" element={<ReposPage />} />
            <Route path="/reports" element={<ReportsPage />} />
            <Route
              path="/settings"
              element={isAdmin ? <SettingsPage /> : profile ? <Navigate to="/overview" replace /> : null}
            />
          </Routes>
        </main>
      </div>
    </div>
  )
}

export default App
