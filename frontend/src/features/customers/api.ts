import { apiRequest } from '@/lib/api/client'
import type { PagedResult } from '@/lib/api/types'
import type { CreateCustomerFormValues, CustomerDto, EditCustomerFormValues } from './types'

export type CustomerSearchParams = {
  search?: string
  activeOnly?: boolean
  page: number
  pageSize: number
}

export function fetchCustomers(params: CustomerSearchParams) {
  return apiRequest<PagedResult<CustomerDto>>('/api/customers', { params })
}

export function fetchCustomer(id: string) {
  return apiRequest<CustomerDto>(`/api/customers/${id}`)
}

export function createCustomer(values: CreateCustomerFormValues) {
  return apiRequest<CustomerDto>('/api/customers', { method: 'POST', body: values })
}

export function updateCustomer(id: string, values: EditCustomerFormValues) {
  return apiRequest<CustomerDto>(`/api/customers/${id}`, { method: 'PUT', body: values })
}

export function deactivateCustomer(id: string) {
  return apiRequest<void>(`/api/customers/${id}`, { method: 'DELETE' })
}

export function activateCustomer(id: string) {
  return apiRequest<void>(`/api/customers/${id}/activate`, { method: 'POST' })
}
