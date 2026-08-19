import { apiRequest } from '@/lib/api/client'
import type { PagedResult } from '@/lib/api/types'
import { toLineRequest } from '@/lib/documents/types'
import type { CreateInvoiceFormValues, InvoiceDto, InvoiceListItemDto } from './types'

export type InvoiceSearchParams = {
  search?: string
  status?: string
  fromDate?: string
  toDate?: string
  customerId?: string
  unpaidOnly?: boolean
  page: number
  pageSize: number
}

export function fetchInvoices(params: InvoiceSearchParams) {
  return apiRequest<PagedResult<InvoiceListItemDto>>('/api/invoices', { params })
}

export function fetchInvoice(id: string) {
  return apiRequest<InvoiceDto>(`/api/invoices/${id}`)
}

export function createInvoice(values: CreateInvoiceFormValues) {
  return apiRequest<InvoiceDto>('/api/invoices', {
    method: 'POST',
    body: {
      // An empty string from the select means "walk-in"; the API expects a real null there.
      customerId: values.customerId || null,
      walkInName: values.walkInName || null,
      invoiceDate: values.invoiceDate,
      paymentMode: values.paymentMode,
      amountPaid: values.amountPaid,
      notes: values.notes || null,
      items: values.items.map(toLineRequest),
    },
  })
}

export function cancelInvoice(id: string) {
  return apiRequest<void>(`/api/invoices/${id}/cancel`, { method: 'POST' })
}
