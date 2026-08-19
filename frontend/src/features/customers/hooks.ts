import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  activateCustomer,
  createCustomer,
  deactivateCustomer,
  fetchCustomer,
  fetchCustomers,
  updateCustomer,
  type CustomerSearchParams,
} from './api'
import type { CreateCustomerFormValues, EditCustomerFormValues } from './types'

const customersKey = (params: CustomerSearchParams) => ['customers', params] as const

export function useCustomers(params: CustomerSearchParams) {
  return useQuery({
    queryKey: customersKey(params),
    queryFn: () => fetchCustomers(params),
    placeholderData: keepPreviousData,
  })
}

export function useCustomer(id: string | undefined) {
  return useQuery({
    queryKey: ['customers', 'detail', id],
    queryFn: () => fetchCustomer(id!),
    enabled: !!id,
  })
}

export function useCreateCustomer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: CreateCustomerFormValues) => createCustomer(values),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['customers'] }),
  })
}

export function useUpdateCustomer(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: EditCustomerFormValues) => updateCustomer(id, values),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['customers'] }),
  })
}

export function useDeactivateCustomer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deactivateCustomer(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['customers'] }),
  })
}

export function useActivateCustomer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => activateCustomer(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['customers'] }),
  })
}
