import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  adjustStock,
  fetchDeadStock,
  fetchRateDrift,
  fetchReorder,
  fetchStock,
  fetchStockMovements,
  fetchStockSummary,
  type StockFilterParams,
  type StockMovementSearchParams,
  type StockSearchParams,
} from './api'
import type { AdjustStockFormValues } from './types'

export function useStock(params: StockSearchParams) {
  return useQuery({
    queryKey: ['stock', params],
    queryFn: () => fetchStock(params),
    placeholderData: keepPreviousData,
  })
}

export function useStockSummary(params: StockFilterParams) {
  return useQuery({
    queryKey: ['stock', 'summary', params],
    queryFn: () => fetchStockSummary(params),
    placeholderData: keepPreviousData,
  })
}

export function useStockMovements(params: StockMovementSearchParams) {
  return useQuery({
    queryKey: ['stock', 'movements', params],
    queryFn: () => fetchStockMovements(params),
    placeholderData: keepPreviousData,
  })
}

export function useAdjustStock() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: AdjustStockFormValues) => adjustStock(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['stock'] })
      // The product master shows stock on hand too, so it goes stale on the same event.
      queryClient.invalidateQueries({ queryKey: ['products'] })
      queryClient.invalidateQueries({ queryKey: ['dashboard'] })
    },
  })
}

/**
 * The three shelf reports. Each sweeps the whole catalogue, so they are held a little while rather
 * than refetched every time somebody flips between the tabs.
 */
export function useDeadStock(months: number) {
  return useQuery({
    queryKey: ['stock', 'dead-stock', months],
    queryFn: () => fetchDeadStock(months),
    staleTime: 60_000,
  })
}

export function useRateDrift(marginFloor: number) {
  return useQuery({
    queryKey: ['stock', 'rate-drift', marginFloor],
    queryFn: () => fetchRateDrift(marginFloor),
    staleTime: 60_000,
  })
}

export function useReorder(coverDays: number) {
  return useQuery({
    queryKey: ['stock', 'reorder', coverDays],
    queryFn: () => fetchReorder(coverDays),
    staleTime: 60_000,
  })
}
