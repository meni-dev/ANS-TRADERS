import { apiRequest } from '@/lib/api/client'
import type { CashBookDto, CashPositionDto, DayCloseDto } from './types'

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
