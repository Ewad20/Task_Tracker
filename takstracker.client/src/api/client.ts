import type {
  ApiError,
  AuditLogDto,
  AuthResponse,
  LoginRequest,
  NotificationDto,
  CreateProjectRequest,
  ProjectDto,
  ProjectReportDto,
  CreateTaskRequest,
  RegisterRequest,
  TaskDto,
  TaskFilterRequest,
  UpdateProjectRequest,
  UpdateTaskRequest,
  UpsertProjectReportRequest,
  ChangePasswordRequest,
  ResetUserPasswordRequest,
  UpdateUserRoleRequest,
  UserProfileDto,
} from './types'

const tokenKey = 'tt_auth_token'
const apiBase = (import.meta.env.VITE_API_BASE_URL as string | undefined) ??
  'http://127.0.0.1:8080'

const buildUrl = (path: string, query?: Record<string, string | number | boolean | undefined>) => {
  const url = new URL(`${apiBase}${path}`)
  if (query) {
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        url.searchParams.append(key, String(value))
      }
    })
  }
  return url.toString()
}

const getToken = () => localStorage.getItem(tokenKey)
const getApiBase = () => apiBase
const setToken = (token: string | null) => {
  if (token) {
    localStorage.setItem(tokenKey, token)
  } else {
    localStorage.removeItem(tokenKey)
  }
  window.dispatchEvent(new Event('auth-changed'))
}

const request = async <T>(
  path: string,
  options: RequestInit = {},
  query?: Record<string, string | number | boolean | undefined>,
): Promise<T> => {
  const token = getToken()
  const response = await fetch(buildUrl(path, query), {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  })

  if (!response.ok) {
    const text = await response.text()
    let message = text
    try {
      const payload = JSON.parse(text) as { error?: string }
      message = payload.error ?? text
    } catch {
      message = text
    }
    const error = new Error(message || response.statusText) as ApiError
    error.status = response.status
    throw error
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

const login = (payload: LoginRequest) =>
  request<AuthResponse>('/api/users/login', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

const register = (payload: RegisterRequest) =>
  request<AuthResponse>('/api/users/register', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

const getProfile = () => request<UserProfileDto>('/api/users/me')

const getUsers = () => request<UserProfileDto[]>('/api/users')

const updateUserRole = (userId: string, payload: UpdateUserRoleRequest) =>
  request<UserProfileDto>(`/api/users/${userId}/role`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })

const changePassword = (payload: ChangePasswordRequest) =>
  request<void>('/api/users/me/password', {
    method: 'PUT',
    body: JSON.stringify(payload),
  })

const resetUserPassword = (userId: string, payload: ResetUserPasswordRequest) =>
  request<void>(`/api/users/${userId}/password`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })

const getProjects = () => request<ProjectDto[]>('/api/projects')

const createProject = (payload: CreateProjectRequest) =>
  request<ProjectDto>('/api/projects', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

const updateProject = (projectId: string, payload: UpdateProjectRequest) =>
  request<ProjectDto>(`/api/projects/${projectId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })

const deleteProject = (projectId: string) =>
  request<void>(`/api/projects/${projectId}`, {
    method: 'DELETE',
  })

const getTasks = (filter?: TaskFilterRequest) =>
  request<TaskDto[]>('/api/tasks', undefined, filter ? { ...filter } : undefined)

const createTask = (payload: CreateTaskRequest) =>
  request<TaskDto>('/api/tasks', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

const updateTask = (taskId: string, payload: UpdateTaskRequest) =>
  request<TaskDto>(`/api/tasks/${taskId}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
  })

const deleteTask = (taskId: string) =>
  request<void>(`/api/tasks/${taskId}`, {
    method: 'DELETE',
  })

const getReports = () => request<ProjectReportDto[]>('/api/reports')

const upsertReport = (payload: UpsertProjectReportRequest) =>
  request<ProjectReportDto>('/api/reports', {
    method: 'POST',
    body: JSON.stringify(payload),
  })

const getNotifications = (projectId?: string) =>
  request<NotificationDto[]>('/api/notifications', undefined, { projectId })

const getAuditLogs = () => request<AuditLogDto[]>('/api/audit')

export const api = {
  getApiBase,
  getToken,
  setToken,
  login,
  register,
  getProfile,
  getUsers,
  updateUserRole,
  changePassword,
  resetUserPassword,
  getProjects,
  createProject,
  updateProject,
  deleteProject,
  getTasks,
  createTask,
  updateTask,
  deleteTask,
  getReports,
  upsertReport,
  getNotifications,
  getAuditLogs,
}
