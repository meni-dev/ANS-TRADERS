import { apiRequest } from '@/lib/api/client'
import type { PagedResult } from '@/lib/api/types'
import type {
  AuditEvent,
  CreatedUser,
  PermissionInfo,
  Role,
  SaveRoleValues,
  SignInResult,
  UserRow,
} from './types'

export function signIn(username: string, password: string) {
  return apiRequest<SignInResult>('/api/auth/sign-in', {
    method: 'POST',
    body: { username, password },
  })
}

export function signOut() {
  return apiRequest<void>('/api/auth/sign-out', { method: 'POST' })
}

export function changePassword(currentPassword: string, newPassword: string) {
  return apiRequest<void>('/api/auth/change-password', {
    method: 'POST',
    body: { currentPassword, newPassword },
  })
}

export function fetchUsers() {
  return apiRequest<UserRow[]>('/api/users')
}

export function createUser(values: { name: string; username: string; roleId: string }) {
  return apiRequest<CreatedUser>('/api/users', { method: 'POST', body: values })
}

export function changeUserRole(id: string, roleId: string) {
  return apiRequest<void>(`/api/users/${id}/role`, { method: 'PUT', body: { roleId } })
}

export function fetchPermissions() {
  return apiRequest<PermissionInfo[]>('/api/roles/permissions')
}

export function fetchRoles() {
  return apiRequest<Role[]>('/api/roles')
}

export function createRole(values: SaveRoleValues) {
  return apiRequest<Role>('/api/roles', { method: 'POST', body: values })
}

export function updateRole(id: string, values: SaveRoleValues) {
  return apiRequest<Role>(`/api/roles/${id}`, { method: 'PUT', body: values })
}

export function deleteRole(id: string) {
  return apiRequest<void>(`/api/roles/${id}`, { method: 'DELETE' })
}

export function resetPassword(id: string) {
  return apiRequest<{ temporaryPassword: string }>(`/api/users/${id}/reset-password`, {
    method: 'POST',
  })
}

export function setUserActive(id: string, isActive: boolean) {
  return apiRequest<void>(`/api/users/${id}/${isActive ? 'activate' : 'deactivate'}`, {
    method: 'POST',
  })
}

export type AuditSearchParams = {
  search?: string
  action?: string
  fromDate?: string
  toDate?: string
  page: number
  pageSize: number
}

export function fetchAudit(params: AuditSearchParams) {
  return apiRequest<PagedResult<AuditEvent>>('/api/audit', { params })
}
