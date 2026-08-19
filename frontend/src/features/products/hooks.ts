import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  confirmProductImport,
  previewProductImport,
  activateProduct,
  createProduct,
  deactivateProduct,
  fetchProduct,
  fetchProducts,
  updateProduct,
  type ProductSearchParams,
} from './api'
import type { CreateProductFormValues, EditProductFormValues, ProductImportRow } from './types'

const productsKey = (params: ProductSearchParams) => ['products', params] as const

export function useProducts(params: ProductSearchParams) {
  return useQuery({
    queryKey: productsKey(params),
    queryFn: () => fetchProducts(params),
    placeholderData: keepPreviousData,
  })
}

export function useProduct(id: string | undefined) {
  return useQuery({
    queryKey: ['products', 'detail', id],
    queryFn: () => fetchProduct(id!),
    enabled: !!id,
  })
}

export function useCreateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: CreateProductFormValues) => createProduct(values),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  })
}

export function useUpdateProduct(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: EditProductFormValues) => updateProduct(id, values),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  })
}

export function useDeactivateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deactivateProduct(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  })
}

export function useActivateProduct() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => activateProduct(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['products'] }),
  })
}

export function usePreviewProductImport() {
  return useMutation({
    mutationFn: (input: { rows: ProductImportRow[]; updateExisting: boolean }) =>
      previewProductImport(input.rows, input.updateExisting),
  })
}

export function useConfirmProductImport() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: { rows: ProductImportRow[]; updateExisting: boolean }) =>
      confirmProductImport(input.rows, input.updateExisting),
    onSuccess: () => {
      // A catalogue load moves the master, the shelf and every screen that reads either.
      for (const key of ['products', 'stock', 'dashboard']) {
        queryClient.invalidateQueries({ queryKey: [key] })
      }
    },
  })
}

