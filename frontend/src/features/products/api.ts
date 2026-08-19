import { apiRequest } from '@/lib/api/client'
import type {
  CreateProductFormValues,
  EditProductFormValues,
  PagedResult,
  ProductDto,
  ProductImportPreviewDto,
  ProductImportResultDto,
  ProductImportRow,
} from './types'

export type ProductSearchParams = {
  search?: string
  activeOnly?: boolean
  page: number
  pageSize: number
}

export function fetchProducts(params: ProductSearchParams) {
  return apiRequest<PagedResult<ProductDto>>('/api/products', { params })
}

export function fetchProduct(id: string) {
  return apiRequest<ProductDto>(`/api/products/${id}`)
}

export function createProduct(values: CreateProductFormValues) {
  return apiRequest<ProductDto>('/api/products', { method: 'POST', body: values })
}

export function updateProduct(id: string, values: EditProductFormValues) {
  return apiRequest<ProductDto>(`/api/products/${id}`, { method: 'PUT', body: values })
}

export function deactivateProduct(id: string) {
  return apiRequest<void>(`/api/products/${id}`, { method: 'DELETE' })
}

export function activateProduct(id: string) {
  return apiRequest<void>(`/api/products/${id}/activate`, { method: 'POST' })
}

export function previewProductImport(rows: ProductImportRow[], updateExisting: boolean) {
  return apiRequest<ProductImportPreviewDto>('/api/products/import/preview', {
    method: 'POST',
    body: { rows, updateExisting },
  })
}

export function confirmProductImport(rows: ProductImportRow[], updateExisting: boolean) {
  return apiRequest<ProductImportResultDto>('/api/products/import', {
    method: 'POST',
    body: { rows, updateExisting },
  })
}

