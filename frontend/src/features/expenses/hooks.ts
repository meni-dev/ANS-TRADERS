import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  cancelExpense,
  createExpense,
  fetchExpenseSummary,
  fetchExpenses,
  fetchProfitAndLoss,
  type ExpenseSearchParams,
  type PeriodParams,
} from './api'
import type { CreateExpenseFormValues } from './types'

/** An expense moves the drawer and the profit figure, so both go stale together. */
function invalidateSpend(queryClient: ReturnType<typeof useQueryClient>) {
  for (const key of ['expenses', 'profit-and-loss', 'payments', 'dashboard']) {
    queryClient.invalidateQueries({ queryKey: [key] })
  }
}

export function useExpenses(params: ExpenseSearchParams) {
  return useQuery({
    queryKey: ['expenses', params],
    queryFn: () => fetchExpenses(params),
    placeholderData: keepPreviousData,
  })
}

export function useExpenseSummary(params: PeriodParams) {
  return useQuery({
    queryKey: ['expenses', 'summary', params],
    queryFn: () => fetchExpenseSummary(params),
    placeholderData: keepPreviousData,
  })
}

export function useProfitAndLoss(params: PeriodParams) {
  return useQuery({
    queryKey: ['profit-and-loss', params],
    queryFn: () => fetchProfitAndLoss(params),
    placeholderData: keepPreviousData,
  })
}

export function useCreateExpense() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: CreateExpenseFormValues) => createExpense(values),
    onSuccess: () => invalidateSpend(queryClient),
  })
}

export function useCancelExpense() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => cancelExpense(id),
    onSuccess: () => invalidateSpend(queryClient),
  })
}
