import { apiRequest } from '@/lib/api/client'
import type { PagedResult } from '@/lib/api/types'
import type {
  BounceChequeFormValues,
  ChequeStatus,
  CustomerAccountSummaryDto,
  DuesSummaryDto,
  OpenDocumentDto,
  PartyStatementDto,
  PaymentDto,
  PaymentListItemDto,
  PaymentSummaryDto,
  RecordPaymentFormValues,
} from './types'

export type PaymentSearchParams = {
  search?: string
  direction?: string
  status?: string
  mode?: string
  customerId?: string
  supplierId?: string
  fromDate?: string
  toDate?: string
  unallocatedOnly?: boolean
  page: number
  pageSize: number
}

export type ChequeSearchParams = {
  status?: ChequeStatus
  fromDate?: string
  toDate?: string
  page: number
  pageSize: number
}

export type StatementParams = {
  fromDate?: string
  toDate?: string
  page: number
  pageSize: number
}

export function fetchPayments(params: PaymentSearchParams) {
  return apiRequest<PagedResult<PaymentListItemDto>>('/api/payments', { params })
}

export function fetchPayment(id: string) {
  return apiRequest<PaymentDto>(`/api/payments/${id}`)
}

export function fetchPaymentSummary(params: { fromDate?: string; toDate?: string }) {
  return apiRequest<PaymentSummaryDto>('/api/payments/summary', { params })
}

export function fetchDues() {
  return apiRequest<DuesSummaryDto>('/api/payments/dues')
}

export function recordPayment(values: RecordPaymentFormValues) {
  return apiRequest<PaymentDto>('/api/payments', { method: 'POST', body: values })
}

export function cancelPayment(id: string) {
  return apiRequest<void>(`/api/payments/${id}/cancel`, { method: 'POST' })
}

export function fetchCheques(params: ChequeSearchParams) {
  return apiRequest<PagedResult<PaymentListItemDto>>('/api/cheques', { params })
}

/**
 * One step along a cheque's life. The server owns which steps are legal and answers 409 for the
 * rest, so nothing here needs to duplicate that table.
 */
export function moveCheque(
  paymentId: string,
  action: 'deposit' | 'clear' | 'post' | 'cancel',
  onDate?: string,
) {
  return apiRequest<PaymentDto>(`/api/cheques/${paymentId}/${action}`, {
    method: 'POST',
    body: { onDate },
  })
}

export function bounceCheque(paymentId: string, values: BounceChequeFormValues) {
  return apiRequest<PaymentDto>(`/api/cheques/${paymentId}/bounce`, {
    method: 'POST',
    body: values,
  })
}

export function fetchPartyStatement(
  party: { customerId?: string; supplierId?: string },
  params: StatementParams,
) {
  const base = party.customerId ? `/api/customers/${party.customerId}` : `/api/suppliers/${party.supplierId}`
  return apiRequest<PartyStatementDto>(`${base}/ledger`, { params })
}

export function fetchOpenDocuments(party: { customerId?: string; supplierId?: string }) {
  const base = party.customerId ? `/api/customers/${party.customerId}` : `/api/suppliers/${party.supplierId}`
  return apiRequest<OpenDocumentDto[]>(`${base}/outstanding`)
}

export function fetchCustomerAccountSummary(customerId: string) {
  return apiRequest<CustomerAccountSummaryDto>(`/api/customers/${customerId}/account-summary`)
}
