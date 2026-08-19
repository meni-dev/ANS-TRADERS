import { documentLineSchema } from '@/lib/documents/types'
import { z } from 'zod'

export type InvoiceItemDto = {
  id: string
  productId: string
  partNumber: string
  itemName: string
  hsn: string
  uqc: string
  quantity: number
  rate: number
  discountPercent: number
  discountAmount: number
  grossAmount: number
  taxableAmount: number
  gstRate: number
  cgstAmount: number
  sgstAmount: number
  igstAmount: number
  lineTotal: number
}

export type InvoiceDto = {
  id: string
  invoiceNumber: string
  financialYear: string
  invoiceDate: string
  /** When payment is expected. Null on a cash bill, or on any invoice raised before terms existed. */
  dueDate?: string | null
  customerId?: string | null
  customerName: string
  customerPhone?: string | null
  customerGstin?: string | null
  customerStateCode?: string | null
  isInterState: boolean
  subTotal: number
  discountAmount: number
  taxableAmount: number
  cgstAmount: number
  sgstAmount: number
  igstAmount: number
  totalTax: number
  roundOff: number
  grandTotal: number
  amountPaid: number
  balanceDue: number
  paymentMode: string
  notes?: string | null
  status: string
  items: InvoiceItemDto[]
  createdAt: string
  updatedAt: string
}

export type InvoiceListItemDto = {
  id: string
  invoiceNumber: string
  invoiceDate: string
  customerId?: string | null
  customerName: string
  customerPhone?: string | null
  itemCount: number
  taxableAmount: number
  totalTax: number
  grandTotal: number
  amountPaid: number
  balanceDue: number
  paymentMode: string
  status: string
}

export const createInvoiceSchema = z
  .object({
    customerId: z.string().optional().or(z.literal('')),
    walkInName: z.string().max(200).optional().or(z.literal('')),
    invoiceDate: z.string().min(1, 'Pick a date'),
    paymentMode: z.string().min(1),
    /** A flat amount off the whole bill — the counter's "make it ₹950". */
    billDiscountAmount: z.number().min(0, 'A discount cannot be negative'),
    amountPaid: z.number('Enter an amount').min(0, 'Cannot be negative'),
    notes: z.string().max(1000).optional().or(z.literal('')),
    items: z.array(documentLineSchema).min(1, 'Add at least one item'),
  })
  // Mirrors the server rule: a bill goes either to an account customer or to a named walk-in, and
  // an unnamed sale cannot be reconciled later.
  .refine((values) => !!values.customerId || !!values.walkInName?.trim(), {
    path: ['walkInName'],
    message: 'Enter a customer name, or pick a saved customer',
  })
  // Caught here so the user is told before the round trip. The server checks stock again against
  // the live figure and is the one that decides — this only spares an avoidable rejection.
  .superRefine((values, ctx) => {
    values.items.forEach((line, index) => {
      if (line.productId && line.quantity > line.stockOnHand) {
        ctx.addIssue({
          code: 'custom',
          path: ['items', index, 'quantity'],
          message: `Only ${line.stockOnHand} in stock`,
        })
      }
    })
  })

export type CreateInvoiceFormValues = z.infer<typeof createInvoiceSchema>
