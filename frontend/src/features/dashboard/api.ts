import { apiRequest } from '@/lib/api/client'
import type { DashboardDto } from './types'

export function fetchDashboard(asOf: string) {
  return apiRequest<DashboardDto>('/api/dashboard', { params: { asOf } })
}
