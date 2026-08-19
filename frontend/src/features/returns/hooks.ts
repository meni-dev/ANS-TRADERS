import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  cancelCreditNote,
  cancelDebitNote,
  createCreditNote,
  createDebitNote,
  fetchCreditNote,
  fetchCreditNotes,
  fetchDebitNote,
  fetchDebitNotes,
  fetchInvoiceReturnable,
  fetchPurchaseReturnable,
  type CreateReturnPayload,
  type ReturnSearchParams,
} from './api'

/**
 * A return touches goods, money and tax at once: stock, the bill it credits, the party's balance,
 * the cash book if it was refunded, and the dashboard's GST and reconciliation panels. Invalidating
 * them piecemeal from each mutation is how a screen ends up showing a figure that moved.
 */
function invalidateReturns(queryClient: ReturnType<typeof useQueryClient>) {
  for (const key of [
    'credit-notes', 'debit-notes', 'returnable',
    'invoices', 'purchases', 'stock', 'products',
    'payments', 'party-statement', 'account-summary', 'open-documents',
    'customers', 'suppliers', 'dashboard',
  ]) {
    queryClient.invalidateQueries({ queryKey: [key] })
  }
}

export function useCreditNotes(params: ReturnSearchParams, enabled = true) {
  return useQuery({
    queryKey: ['credit-notes', params],
    queryFn: () => fetchCreditNotes(params),
    enabled,
    placeholderData: keepPreviousData,
  })
}

export function useCreditNote(id: string | undefined) {
  return useQuery({
    queryKey: ['credit-notes', 'detail', id],
    queryFn: () => fetchCreditNote(id!),
    enabled: Boolean(id),
  })
}

export function useInvoiceReturnable(invoiceId: string | undefined) {
  return useQuery({
    queryKey: ['returnable', 'invoice', invoiceId],
    queryFn: () => fetchInvoiceReturnable(invoiceId!),
    enabled: Boolean(invoiceId),
  })
}

export function useCreateCreditNote() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { invoiceId: string; payload: CreateReturnPayload }) =>
      createCreditNote(input.invoiceId, input.payload),
    onSuccess: () => invalidateReturns(queryClient),
  })
}

export function useCancelCreditNote() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => cancelCreditNote(id),
    onSuccess: () => invalidateReturns(queryClient),
  })
}

export function useDebitNotes(params: ReturnSearchParams, enabled = true) {
  return useQuery({
    queryKey: ['debit-notes', params],
    queryFn: () => fetchDebitNotes(params),
    enabled,
    placeholderData: keepPreviousData,
  })
}

export function useDebitNote(id: string | undefined) {
  return useQuery({
    queryKey: ['debit-notes', 'detail', id],
    queryFn: () => fetchDebitNote(id!),
    enabled: Boolean(id),
  })
}

export function usePurchaseReturnable(purchaseId: string | undefined) {
  return useQuery({
    queryKey: ['returnable', 'purchase', purchaseId],
    queryFn: () => fetchPurchaseReturnable(purchaseId!),
    enabled: Boolean(purchaseId),
  })
}

export function useCreateDebitNote() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { purchaseId: string; payload: CreateReturnPayload }) =>
      createDebitNote(input.purchaseId, input.payload),
    onSuccess: () => invalidateReturns(queryClient),
  })
}

export function useCancelDebitNote() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => cancelDebitNote(id),
    onSuccess: () => invalidateReturns(queryClient),
  })
}
