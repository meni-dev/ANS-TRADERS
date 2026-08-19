import type { ShopSettingsDto } from '@/features/settings/types'
import type { InvoiceDto, InvoiceItemDto } from '../types'

/** The seller's address as one line, skipping whatever is not filled in. */
export function shopAddress(shop: ShopSettingsDto): string {
  return [
    shop.addressLine1,
    shop.addressLine2,
    [shop.city, shop.pincode].filter(Boolean).join(' '),
    shop.state,
  ]
    .filter((part) => part && part.trim())
    .join(', ')
}

export type TaxRateRow = {
  gstRate: number
  taxableValue: number
  cgstAmount: number
  sgstAmount: number
  igstAmount: number
  totalTax: number
}

/**
 * The rate-wise tax summary an accountant reads off the foot of a bill: one row per GST rate, so a
 * bill mixing 18% and 28% parts can be checked against the return without adding up lines by hand.
 */
export function taxByRate(items: InvoiceItemDto[]): TaxRateRow[] {
  const rows = new Map<number, TaxRateRow>()

  for (const item of items) {
    const row = rows.get(item.gstRate) ?? {
      gstRate: item.gstRate,
      taxableValue: 0,
      cgstAmount: 0,
      sgstAmount: 0,
      igstAmount: 0,
      totalTax: 0,
    }

    row.taxableValue += item.taxableAmount
    row.cgstAmount += item.cgstAmount
    row.sgstAmount += item.sgstAmount
    row.igstAmount += item.igstAmount
    row.totalTax += item.cgstAmount + item.sgstAmount + item.igstAmount

    rows.set(item.gstRate, row)
  }

  return [...rows.values()].sort((a, b) => a.gstRate - b.gstRate)
}

/** Bill-to lines, with the fallback every template uses for an unregistered walk-in. */
export function customerLines(invoice: InvoiceDto): string[] {
  return [
    invoice.customerPhone,
    invoice.customerGstin ? `GSTIN ${invoice.customerGstin}` : 'Unregistered (B2C)',
  ].filter((line): line is string => !!line)
}
