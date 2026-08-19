import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  cancelInvoice,
  createInvoice,
  fetchInvoice,
  fetchInvoices,
  type InvoiceSearchParams,
} from './api'
import type { CreateInvoiceFormValues } from './types'

export function useInvoices(params: InvoiceSearchParams) {
  return useQuery({
    queryKey: ['invoices', params],
    queryFn: () => fetchInvoices(params),
    placeholderData: keepPreviousData,
  })
}

export function useInvoice(id: string | undefined) {
  return useQuery({
    queryKey: ['invoices', 'detail', id],
    queryFn: () => fetchInvoice(id!),
    enabled: !!id,
  })
}

export function useCreateInvoice() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: CreateInvoiceFormValues) => createInvoice(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
      // Both documents move stock, so the stock screens and the product master go stale with them.
      queryClient.invalidateQueries({ queryKey: ['stock'] })
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}

export function useCancelInvoice() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => cancelInvoice(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
      // Both documents move stock, so the stock screens and the product master go stale with them.
      queryClient.invalidateQueries({ queryKey: ['stock'] })
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}
