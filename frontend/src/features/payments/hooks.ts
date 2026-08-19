import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  bounceCheque,
  cancelPayment,
  fetchCheques,
  fetchCustomerAccountSummary,
  fetchDues,
  fetchOpenDocuments,
  fetchPartyStatement,
  fetchPayment,
  fetchPaymentSummary,
  fetchPayments,
  moveCheque,
  recordPayment,
  type ChequeSearchParams,
  type PaymentSearchParams,
  type StatementParams,
} from './api'
import type { BounceChequeFormValues, RecordPaymentFormValues } from './types'

/**
 * Everything money touches. A receipt changes a party's balance, the bills it settled, the cheque
 * register, the day's collections and the dashboard — invalidating them one by one from each
 * mutation is how a screen ends up showing yesterday's figure.
 */
function invalidateMoney(queryClient: ReturnType<typeof useQueryClient>) {
  for (const key of ['payments', 'cheques', 'party-statement', 'account-summary', 'open-documents']) {
    queryClient.invalidateQueries({ queryKey: [key] })
  }
  queryClient.invalidateQueries({ queryKey: ['invoices'] })
  queryClient.invalidateQueries({ queryKey: ['purchases'] })
  queryClient.invalidateQueries({ queryKey: ['customers'] })
  queryClient.invalidateQueries({ queryKey: ['suppliers'] })
  queryClient.invalidateQueries({ queryKey: ['dashboard'] })
}

export function usePayments(params: PaymentSearchParams) {
  return useQuery({
    queryKey: ['payments', params],
    queryFn: () => fetchPayments(params),
    placeholderData: keepPreviousData,
  })
}

export function usePayment(id: string | undefined) {
  return useQuery({
    queryKey: ['payments', 'detail', id],
    queryFn: () => fetchPayment(id!),
    enabled: Boolean(id),
  })
}

export function usePaymentSummary(params: { fromDate?: string; toDate?: string }) {
  return useQuery({
    queryKey: ['payments', 'summary', params],
    queryFn: () => fetchPaymentSummary(params),
    placeholderData: keepPreviousData,
  })
}

export function useDues() {
  return useQuery({ queryKey: ['payments', 'dues'], queryFn: fetchDues })
}

export function useRecordPayment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: RecordPaymentFormValues) => recordPayment(values),
    onSuccess: () => invalidateMoney(queryClient),
  })
}

export function useCancelPayment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => cancelPayment(id),
    onSuccess: () => invalidateMoney(queryClient),
  })
}

export function useCheques(params: ChequeSearchParams) {
  return useQuery({
    queryKey: ['cheques', params],
    queryFn: () => fetchCheques(params),
    placeholderData: keepPreviousData,
  })
}

export function useMoveCheque() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: {
      paymentId: string
      action: 'deposit' | 'clear' | 'post' | 'cancel'
      onDate?: string
    }) => moveCheque(input.paymentId, input.action, input.onDate),
    onSuccess: () => invalidateMoney(queryClient),
  })
}

export function useBounceCheque() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { paymentId: string; values: BounceChequeFormValues }) =>
      bounceCheque(input.paymentId, input.values),
    onSuccess: () => invalidateMoney(queryClient),
  })
}

export function usePartyStatement(
  party: { customerId?: string; supplierId?: string },
  params: StatementParams,
) {
  return useQuery({
    queryKey: ['party-statement', party, params],
    queryFn: () => fetchPartyStatement(party, params),
    enabled: Boolean(party.customerId ?? party.supplierId),
    placeholderData: keepPreviousData,
  })
}

export function useOpenDocuments(party: { customerId?: string; supplierId?: string }) {
  return useQuery({
    queryKey: ['open-documents', party],
    queryFn: () => fetchOpenDocuments(party),
    enabled: Boolean(party.customerId ?? party.supplierId),
  })
}

/**
 * Fetched only once a customer is chosen, and never folded into the customer list: the list would
 * then pay for cheque and ageing aggregates on every row of every page.
 */
export function useCustomerAccountSummary(customerId: string | undefined) {
  return useQuery({
    queryKey: ['account-summary', customerId],
    queryFn: () => fetchCustomerAccountSummary(customerId!),
    enabled: Boolean(customerId),
  })
}
