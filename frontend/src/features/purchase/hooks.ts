import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  cancelPurchase,
  createPurchase,
  fetchPurchase,
  fetchPurchases,
  type PurchaseSearchParams,
} from './api'
import type { CreatePurchaseFormValues } from './types'

export function usePurchases(params: PurchaseSearchParams) {
  return useQuery({
    queryKey: ['purchases', params],
    queryFn: () => fetchPurchases(params),
    placeholderData: keepPreviousData,
  })
}

export function usePurchase(id: string | undefined) {
  return useQuery({
    queryKey: ['purchases', 'detail', id],
    queryFn: () => fetchPurchase(id!),
    enabled: !!id,
  })
}

export function useCreatePurchase() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: CreatePurchaseFormValues) => createPurchase(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchases'] })
      // Both documents move stock, so the stock screens and the product master go stale with them.
      queryClient.invalidateQueries({ queryKey: ['stock'] })
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}

export function useCancelPurchase() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => cancelPurchase(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchases'] })
      // Both documents move stock, so the stock screens and the product master go stale with them.
      queryClient.invalidateQueries({ queryKey: ['stock'] })
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}
