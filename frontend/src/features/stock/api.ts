import { apiRequest } from '@/lib/api/client'
import type { PagedResult } from '@/lib/api/types'
import type {
  AdjustStockFormValues,
  DeadStockReport,
  ProductStockDto,
  RateDriftReport,
  ReorderReport,
  StockMovementDto,
  StockSummaryDto,
} from './types'

export type StockSearchParams = {
  search?: string
  lowOnly?: boolean
  activeOnly?: boolean
  page: number
  pageSize: number
}

/** The filter half of {@link StockSearchParams}, shared with the summary endpoint. */
export type StockFilterParams = Omit<StockSearchParams, 'page' | 'pageSize'>

export type StockMovementSearchParams = {
  search?: string
  productId?: string
  movementType?: string
  fromDate?: string
  toDate?: string
  page: number
  pageSize: number
}

export function fetchStock(params: StockSearchParams) {
  return apiRequest<PagedResult<ProductStockDto>>('/api/stock', { params })
}

export function fetchStockSummary(params: StockFilterParams) {
  return apiRequest<StockSummaryDto>('/api/stock/summary', { params })
}

export function fetchStockMovements(params: StockMovementSearchParams) {
  return apiRequest<PagedResult<StockMovementDto>>('/api/stock/movements', { params })
}

export function adjustStock(values: AdjustStockFormValues) {
  return apiRequest<ProductStockDto>('/api/stock/adjust', { method: 'POST', body: values })
}

export function fetchDeadStock(months: number) {
  return apiRequest<DeadStockReport>('/api/stock/dead-stock', { params: { months } })
}

export function fetchRateDrift(marginFloor: number) {
  return apiRequest<RateDriftReport>('/api/stock/rate-drift', { params: { marginFloor } })
}

export function fetchReorder(coverDays: number) {
  return apiRequest<ReorderReport>('/api/stock/reorder', { params: { coverDays } })
}
