import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  changePassword,
  changeUserRole,
  createRole,
  createUser,
  deleteRole,
  fetchAudit,
  fetchPermissions,
  fetchRoles,
  fetchUsers,
  resetPassword,
  setUserActive,
  updateRole,
  type AuditSearchParams,
} from './api'
import type { SaveRoleValues } from './types'

/** Roles and people move together: changing one changes what the other screen shows. */
function invalidatePeople(queryClient: ReturnType<typeof useQueryClient>) {
  for (const key of ['users', 'roles']) {
    queryClient.invalidateQueries({ queryKey: [key] })
  }
}

export function useUsers() {
  return useQuery({ queryKey: ['users'], queryFn: fetchUsers })
}

export function useAudit(params: AuditSearchParams) {
  return useQuery({
    queryKey: ['audit', params],
    queryFn: () => fetchAudit(params),
    placeholderData: keepPreviousData,
  })
}

export function useCreateUser() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: { name: string; username: string; roleId: string }) => createUser(values),
    onSuccess: () => invalidatePeople(queryClient),
  })
}

export function useChangeUserRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, roleId }: { id: string; roleId: string }) => changeUserRole(id, roleId),
    onSuccess: () => invalidatePeople(queryClient),
  })
}

/** The permission list is code, so it cannot change while the tab is open. */
export function usePermissionCatalogue() {
  return useQuery({ queryKey: ['permissions'], queryFn: fetchPermissions, staleTime: Infinity })
}

export function useRoles() {
  return useQuery({ queryKey: ['roles'], queryFn: fetchRoles })
}

export function useCreateRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: SaveRoleValues) => createRole(values),
    onSuccess: () => invalidatePeople(queryClient),
  })
}

export function useUpdateRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, values }: { id: string; values: SaveRoleValues }) => updateRole(id, values),
    onSuccess: () => invalidatePeople(queryClient),
  })
}

export function useDeleteRole() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteRole(id),
    onSuccess: () => invalidatePeople(queryClient),
  })
}

export function useResetPassword() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => resetPassword(id),
    onSuccess: () => invalidatePeople(queryClient),
  })
}

export function useSetUserActive() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => setUserActive(id, isActive),
    onSuccess: () => invalidatePeople(queryClient),
  })
}

export function useChangePassword() {
  return useMutation({
    mutationFn: ({ currentPassword, newPassword }: { currentPassword: string; newPassword: string }) =>
      changePassword(currentPassword, newPassword),
  })
}
