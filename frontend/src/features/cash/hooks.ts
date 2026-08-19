import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { closeDay, fetchCashBook, fetchCashPosition } from './api'

export function useCashPosition(date?: string) {
  return useQuery({
    queryKey: ['cash', 'position', date],
    queryFn: () => fetchCashPosition(date),
  })
}

export function useCashBook(params: { fromDate?: string; toDate?: string }) {
  return useQuery({
    queryKey: ['cash', 'book', params],
    queryFn: () => fetchCashBook(params),
    placeholderData: keepPreviousData,
  })
}

export function useCloseDay() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: closeDay,
    // A close resets the running balance, so both cash views move together.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['cash'] }),
  })
}
