import { apiRequest } from '@/lib/api/client'
import type { PagedResult } from '@/lib/api/types'
import type {
  CreateExpenseFormValues,
  ExpenseDto,
  ExpenseSummaryDto,
  ProfitAndLossDto,
} from './types'

export type ExpenseSearchParams = {
  search?: string
  category?: string
  fromDate?: string
  toDate?: string
  page: number
  pageSize: number
}

export type PeriodParams = { fromDate?: string; toDate?: string }

export function fetchExpenses(params: ExpenseSearchParams) {
  return apiRequest<PagedResult<ExpenseDto>>('/api/expenses', { params })
}

export function fetchExpenseSummary(params: PeriodParams) {
  return apiRequest<ExpenseSummaryDto>('/api/expenses/summary', { params })
}

export function fetchProfitAndLoss(params: PeriodParams) {
  return apiRequest<ProfitAndLossDto>('/api/expenses/profit-and-loss', { params })
}

export function createExpense(values: CreateExpenseFormValues) {
  return apiRequest<ExpenseDto>('/api/expenses', { method: 'POST', body: values })
}

export function cancelExpense(id: string) {
  return apiRequest<void>(`/api/expenses/${id}/cancel`, { method: 'POST' })
}
