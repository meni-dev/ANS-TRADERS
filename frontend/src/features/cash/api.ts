import { apiRequest } from '@/lib/api/client'
import type {
  CapitalSummary,
  CashBookDto,
  CashPositionDto,
  DayCloseDto,
  MoneyMovement,
} from './types'

export function fetchCashPosition(date?: string) {
  return apiRequest<CashPositionDto>('/api/cash/position', { params: { date } })
}

export function fetchCashBook(params: { fromDate?: string; toDate?: string }) {
  return apiRequest<CashBookDto>('/api/cash/book', { params })
}

export function closeDay(body: {
  closeDate: string
  countedCash: number
  reason?: string
  notes?: string
}) {
  return apiRequest<DayCloseDto>('/api/cash/close', { method: 'POST', body })
}
export function fetchMoneyMovements(fromDate: string, toDate: string) {
  return apiRequest<MoneyMovement[]>('/api/money', { params: { fromDate, toDate } })
}

export function recordMoneyMovement(body: {
  movementDate: string
  kind: string
  amount: number
  affectsCash: boolean
  referenceNumber?: string | null
  notes?: string | null
}) {
  return apiRequest<MoneyMovement>('/api/money', { method: 'POST', body })
}

export function cancelMoneyMovement(id: string) {
  return apiRequest<void>(`/api/money/${id}/cancel`, { method: 'POST' })
}

export function fetchCapitalSummary() {
  return apiRequest<CapitalSummary>('/api/money/capital')
}
