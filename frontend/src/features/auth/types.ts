/**
 * Mirrors `Domain.Enums.Permission`. Kept as a union rather than a bare string so a typo in a
 * `useCan` call is a build error instead of a button that silently never appears.
 */
export type Permission =
  | 'BillCreate'
  | 'BillCancel'
  | 'SalesReturn'
  | 'BillDiscount'
  | 'PurchaseView'
  | 'PurchaseCreate'
  | 'PurchaseCancel'
  | 'PurchaseReturn'
  | 'StockView'
  | 'StockAdjust'
  | 'ProductManage'
  | 'PaymentRecord'
  | 'PaymentCancel'
  | 'ExpenseRecord'
  | 'CashDayClose'
  | 'CapitalMovement'
  | 'CostView'
  | 'ReportView'
  | 'UserManage'
  | 'SettingsEdit'
  | 'BooksLock'
  | 'AuditView'

export type SignedInUser = {
  id: string
  name: string
  username: string
  roleId: string
  roleName: string
  mustChangePassword: boolean
  permissions: Permission[]
}

export type PermissionInfo = {
  value: Permission
  label: string
  group: string
  description: string
}

export type Role = {
  id: string
  name: string
  description: string | null
  /** The built-in role. It holds everything and cannot be edited or deleted. */
  isSystem: boolean
  permissions: Permission[]
  userCount: number
}

export type SaveRoleValues = {
  name: string
  description?: string | null
  permissions: Permission[]
}

export type SignInResult = {
  token: string
  expiresAt: string
  user: SignedInUser
}

export type UserRow = {
  id: string
  name: string
  username: string
  roleId: string
  roleName: string
  isActive: boolean
  mustChangePassword: boolean
  lastSignedInAt: string | null
}

export type CreatedUser = {
  user: SignedInUser
  temporaryPassword: string
}

export type AuditEvent = {
  id: string
  occurredAt: string
  userName: string
  action: string
  actionLabel: string
  entityType: string
  entityId: string | null
  entityLabel: string | null
  detail: string | null
}
