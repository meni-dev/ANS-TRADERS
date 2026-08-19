import { z } from 'zod'

/** Mirrors `Domain.Enums.PaymentMode`. Values are sent to the API verbatim. */
export const PAYMENT_MODES = [
  { value: 'Cash', label: 'Cash' },
  { value: 'Upi', label: 'UPI' },
  { value: 'Card', label: 'Card' },
  { value: 'BankTransfer', label: 'Bank Transfer' },
  { value: 'Credit', label: 'Credit (unpaid)' },
] as const

export type PaymentMode = (typeof PAYMENT_MODES)[number]['value']

/**
 * One editable row on a purchase or an invoice. The product columns are carried in form state so
 * the grid can show HSN and GST without re-fetching the item on every keystroke; only the four
 * user-entered fields are actually sent to the API.
 */
export const documentLineSchema = z.object({
  productId: z.string().min(1, 'Pick a product'),
  partNumber: z.string(),
  itemName: z.string(),
  hsn: z.string(),
  uqc: z.string(),
  gstRate: z.number(),
  /** Snapshot of stock at the moment the product was picked; the server re-checks on submit. */
  stockOnHand: z.number(),
  quantity: z.number('Enter a quantity').gt(0, 'Must be more than zero'),
  rate: z.number('Enter a rate').min(0, 'Cannot be negative'),
  discountPercent: z.number('Enter a discount').min(0, 'Cannot be negative').max(100, 'Cannot exceed 100'),
})

export type DocumentLineValues = z.infer<typeof documentLineSchema>

/** Shape the API expects for a line — the display-only snapshot columns are dropped. */
export function toLineRequest(line: DocumentLineValues) {
  return {
    productId: line.productId,
    quantity: line.quantity,
    rate: line.rate,
    discountPercent: line.discountPercent,
  }
}

export const emptyLine: DocumentLineValues = {
  productId: '',
  partNumber: '',
  itemName: '',
  hsn: '',
  uqc: 'PCS',
  gstRate: 0,
  stockOnHand: 0,
  quantity: 1,
  rate: 0,
  discountPercent: 0,
}

/** Document status strings as the API returns them, for both purchases and invoices. */
export type DocumentStatus = 'Received' | 'Issued' | 'Cancelled'
