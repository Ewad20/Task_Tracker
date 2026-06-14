export interface AuthResponse {
  token: string
  expiresAt: string
}

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface UserProfileDto {
  id: string
  userId: string
  displayName: string
  bio: string
  role: string
}

export interface UpdateUserRoleRequest {
  role: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface ResetUserPasswordRequest {
  newPassword: string
}

export interface ProjectDto {
  id: string
  name: string
  description: string
  ownerId: string
  createdAt: string
  members: ProjectMemberDto[]
}

export interface ProjectMemberDto {
  id: string
  userId: string
  role: string
}

export interface CreateProjectRequest {
  name: string
  description: string
  memberUserIds: string[]
}

export type UpdateProjectRequest = CreateProjectRequest

export interface TaskDto {
  id: string
  projectId: string
  title: string
  description: string
  assigneeId: string
  priority: number
  status: number
  dueDate?: string | null
  createdAt: string
}

export interface CreateTaskRequest {
  projectId: string
  title: string
  description: string
  assigneeId: string
  priority: number
  dueDate?: string | null
}

export interface UpdateTaskRequest {
  title: string
  description: string
  assigneeId: string
  priority: number
  status: number
  dueDate?: string | null
}

export interface ProjectReportDto {
  id: string
  projectId: string
  totalTasks: number
  completedTasks: number
  progressPercent: number
  updatedAt: string
}

export interface UpsertProjectReportRequest {
  projectId: string
  totalTasks: number
  completedTasks: number
}

export interface NotificationDto {
  id: string
  userId: string
  message: string
  projectId?: string | null
  taskId?: string | null
  eventType: string
  isRead: boolean
  createdAt: string
}

export interface AuditLogDto {
  id: string
  userId: string
  action: string
  entityType: string
  payload: string
  createdAt: string
}

export interface TaskFilterRequest {
  projectId?: string
  assigneeId?: string
  status?: number
  priority?: number
  search?: string
}

export interface ApiError extends Error {
  status?: number
}
