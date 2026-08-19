import type { ShopSettingsDto } from '@/features/settings/types'
import type { InvoiceDto } from '../types'

/**
 * Every template renders the same document from the same two inputs. Templates differ in layout,
 * density and styling — never in which legally required field appears, because a bill that drops
 * the GSTIN or the tax split is not a valid tax invoice whatever it looks like.
 */
export type InvoiceTemplateProps = {
  invoice: InvoiceDto
  shop: ShopSettingsDto
}

/** Mirrors `Domain.Enums.InvoiceTemplate`. */
export type InvoiceTemplateId = 'Classic' | 'Detailed' | 'Modern' | 'Traditional' | 'Minimal'
