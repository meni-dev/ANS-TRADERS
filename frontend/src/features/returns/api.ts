import { apiRequest } from '@/lib/api/client'
import type { PagedResult } from '@/lib/api/types'
import type {
  CreditNoteDto,
  CreditNoteListItemDto,
  DebitNoteDto,
  DebitNoteListItemDto,
  ReturnableDocumentDto,
} from './types'

export type ReturnSearchParams = {
  search?: string
  customerId?: string
  supplierId?: string
  invoiceId?: string
  purchaseId?: string
  fromDate?: string
  toDate?: string
  page: number
  pageSize: number
}

/** Only quantity goes up — see the note on `returnLineSchema`. */
export type CreateReturnPayload = {
  noteDate: string
  reason: string
  lines: { documentItemId: string; quantity: number }[]
  refundAmount?: number
  refundMode?: string
  refundReference?: string
}

export function fetchCreditNotes(params: ReturnSearchParams) {
  return apiRequest<PagedResult<CreditNoteListItemDto>>('/api/credit-notes', { params })
}

export function fetchCreditNote(id: string) {
  return apiRequest<CreditNoteDto>(`/api/credit-notes/${id}`)
}

export function createCreditNote(invoiceId: string, payload: CreateReturnPayload) {
  return apiRequest<CreditNoteDto>('/api/credit-notes', {
    method: 'POST',
    body: { invoiceId, ...payload },
  })
}

export function cancelCreditNote(id: string) {
  return apiRequest<void>(`/api/credit-notes/${id}/cancel`, { method: 'POST' })
}

export function fetchInvoiceReturnable(invoiceId: string) {
  return apiRequest<ReturnableDocumentDto>(`/api/invoices/${invoiceId}/returnable`)
}

export function fetchDebitNotes(params: ReturnSearchParams) {
  return apiRequest<PagedResult<DebitNoteListItemDto>>('/api/debit-notes', { params })
}

export function fetchDebitNote(id: string) {
  return apiRequest<DebitNoteDto>(`/api/debit-notes/${id}`)
}

export function createDebitNote(purchaseId: string, payload: CreateReturnPayload) {
  return apiRequest<DebitNoteDto>('/api/debit-notes', {
    method: 'POST',
    body: { purchaseId, ...payload },
  })
}

export function cancelDebitNote(id: string) {
  return apiRequest<void>(`/api/debit-notes/${id}/cancel`, { method: 'POST' })
}

export function fetchPurchaseReturnable(purchaseId: string) {
  return apiRequest<ReturnableDocumentDto>(`/api/purchases/${purchaseId}/returnable`)
}
