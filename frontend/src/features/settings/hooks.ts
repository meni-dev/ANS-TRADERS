import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchShopSettings, setBooksLock, updateShopSettings } from './api'
import type { ShopSettingsFormValues } from './types'

/**
 * The shop's own identity. Read by every printed bill and by both document forms, which use its
 * state code to decide IGST against CGST + SGST, so it is cached hard and invalidated on save.
 */
export function useShopSettings() {
  return useQuery({
    queryKey: ['settings'],
    queryFn: fetchShopSettings,
    staleTime: 5 * 60_000,
  })
}

export function useUpdateShopSettings() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (values: ShopSettingsFormValues) => updateShopSettings(values),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['settings'] }),
  })
}

/** Moving the lock changes what every form will accept, so the audit trail goes stale with it. */
export function useSetBooksLock() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (lockedUpTo: string | null) => setBooksLock(lockedUpTo),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] })
      queryClient.invalidateQueries({ queryKey: ['audit'] })
    },
  })
}
