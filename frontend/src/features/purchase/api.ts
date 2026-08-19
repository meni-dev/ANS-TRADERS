import { apiRequest } from '@/lib/api/client'
import type { PagedResult } from '@/lib/api/types'
import { toLineRequest } from '@/lib/documents/types'
import type { CreatePurchaseFormValues, PurchaseDto, PurchaseListItemDto } from './types'

export type PurchaseSearchParams = {
  search?: string
  status?: string
  fromDate?: string
  toDate?: string
  supplierId?: string
  page: number
  pageSize: number
}

export function fetchPurchases(params: PurchaseSearchParams) {
  return apiRequest<PagedResult<PurchaseListItemDto>>('/api/purchases', { params })
}

export function fetchPurchase(id: string) {
  return apiRequest<PurchaseDto>(`/api/purchases/${id}`)
}

export function createPurchase(values: CreatePurchaseFormValues) {
  return apiRequest<PurchaseDto>('/api/purchases', {
    method: 'POST',
    // The line snapshot columns exist only to render the form; the server derives them itself.
    body: { ...values, items: values.items.map(toLineRequest) },
  })
}

export function cancelPurchase(id: string) {
  return apiRequest<void>(`/api/purchases/${id}/cancel`, { method: 'POST' })
}
