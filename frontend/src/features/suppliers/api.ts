import { apiRequest } from '@/lib/api/client'
import type { PagedResult } from '@/lib/api/types'
import type { CreateSupplierFormValues, EditSupplierFormValues, SupplierDto } from './types'

export type SupplierSearchParams = {
  search?: string
  activeOnly?: boolean
  page: number
  pageSize: number
}

export function fetchSuppliers(params: SupplierSearchParams) {
  return apiRequest<PagedResult<SupplierDto>>('/api/suppliers', { params })
}

export function fetchSupplier(id: string) {
  return apiRequest<SupplierDto>(`/api/suppliers/${id}`)
}

export function createSupplier(values: CreateSupplierFormValues) {
  return apiRequest<SupplierDto>('/api/suppliers', { method: 'POST', body: values })
}

export function updateSupplier(id: string, values: EditSupplierFormValues) {
  return apiRequest<SupplierDto>(`/api/suppliers/${id}`, { method: 'PUT', body: values })
}

export function deactivateSupplier(id: string) {
  return apiRequest<void>(`/api/suppliers/${id}`, { method: 'DELETE' })
}

export function activateSupplier(id: string) {
  return apiRequest<void>(`/api/suppliers/${id}/activate`, { method: 'POST' })
}
