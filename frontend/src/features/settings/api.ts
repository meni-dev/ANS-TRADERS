import { apiRequest } from '@/lib/api/client'
import type { ShopSettingsDto, ShopSettingsFormValues } from './types'

export function fetchShopSettings() {
  return apiRequest<ShopSettingsDto>('/api/settings')
}

export function updateShopSettings(values: ShopSettingsFormValues) {
  return apiRequest<ShopSettingsDto>('/api/settings', { method: 'PUT', body: values })
}

export function setBooksLock(lockedUpTo: string | null) {
  return apiRequest<ShopSettingsDto>('/api/settings/books-lock', {
    method: 'PUT',
    body: { lockedUpTo },
  })
}
