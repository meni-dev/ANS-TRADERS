import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  cancelMoneyMovement,
  closeDay,
  fetchCapitalSummary,
  fetchCashBook,
  fetchCashPosition,
  fetchMoneyMovements,
  recordMoneyMovement,
} from './api'

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

/** Money with no party behind it moves the drawer, so both go stale together. */
function invalidateCash(queryClient: ReturnType<typeof useQueryClient>) {
  for (const key of ['cash', 'money', 'dashboard']) {
    queryClient.invalidateQueries({ queryKey: [key] })
  }
}

export function useMoneyMovements(fromDate: string, toDate: string) {
  return useQuery({
    queryKey: ['money', fromDate, toDate],
    queryFn: () => fetchMoneyMovements(fromDate, toDate),
    placeholderData: keepPreviousData,
  })
}

export function useCapitalSummary() {
  return useQuery({ queryKey: ['money', 'capital'], queryFn: fetchCapitalSummary })
}

export function useRecordMoneyMovement() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: recordMoneyMovement,
    onSuccess: () => invalidateCash(queryClient),
  })
}

export function useCancelMoneyMovement() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => cancelMoneyMovement(id),
    onSuccess: () => invalidateCash(queryClient),
  })
}
