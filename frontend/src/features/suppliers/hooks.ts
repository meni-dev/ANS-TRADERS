import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  activateSupplier,
  createSupplier,
  deactivateSupplier,
  fetchSupplier,
  fetchSuppliers,
  updateSupplier,
  type SupplierSearchParams,
} from './api'
import type { CreateSupplierFormValues, EditSupplierFormValues } from './types'

const suppliersKey = (params: SupplierSearchParams) => ['suppliers', params] as const

export function useSuppliers(params: SupplierSearchParams) {
  return useQuery({
    queryKey: suppliersKey(params),
    queryFn: () => fetchSuppliers(params),
    placeholderData: keepPreviousData,
  })
}

export function useSupplier(id: string | undefined) {
  return useQuery({
    queryKey: ['suppliers', 'detail', id],
    queryFn: () => fetchSupplier(id!),
    enabled: !!id,
  })
}

export function useCreateSupplier() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: CreateSupplierFormValues) => createSupplier(values),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['suppliers'] }),
  })
}

export function useUpdateSupplier(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: EditSupplierFormValues) => updateSupplier(id, values),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['suppliers'] }),
  })
}

export function useDeactivateSupplier() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deactivateSupplier(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['suppliers'] }),
  })
}

export function useActivateSupplier() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => activateSupplier(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['suppliers'] }),
  })
}
