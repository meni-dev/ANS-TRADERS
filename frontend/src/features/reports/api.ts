import { apiRequest } from '@/lib/api/client'
import type { Register, RegisterSummary } from './types'

export function fetchRegisters() {
  return apiRequest<RegisterSummary[]>('/api/reports/registers')
}

export function fetchRegister(key: string, fromDate: string, toDate: string) {
  return apiRequest<Register>(`/api/reports/registers/${key}`, { params: { fromDate, toDate } })
}
