import { documentLineSchema } from '@/lib/documents/types'
import { z } from 'zod'

export type PurchaseItemDto = {
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

export type PurchaseDto = {
  id: string
  purchaseNumber: string
  financialYear: string
  supplierInvoiceNumber: string
  invoiceDate: string
  supplierId: string
  supplierName: string
  supplierGstin?: string | null
  supplierStateCode?: string | null
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
  items: PurchaseItemDto[]
  createdAt: string
  updatedAt: string
}

export type PurchaseListItemDto = {
  id: string
  purchaseNumber: string
  supplierInvoiceNumber: string
  invoiceDate: string
  supplierId: string
  supplierName: string
  itemCount: number
  taxableAmount: number
  totalTax: number
  grandTotal: number
  amountPaid: number
  balanceDue: number
  paymentMode: string
  status: string
}

export const createPurchaseSchema = z.object({
  supplierId: z.string().min(1, 'Pick a supplier'),
  supplierInvoiceNumber: z.string().min(1, "Enter the supplier's bill number").max(50),
  invoiceDate: z.string().min(1, 'Pick a date'),
  paymentMode: z.string().min(1),
  amountPaid: z.number('Enter an amount').min(0, 'Cannot be negative'),
  notes: z.string().max(1000).optional().or(z.literal('')),
  items: z.array(documentLineSchema).min(1, 'Add at least one item'),
})

export type CreatePurchaseFormValues = z.infer<typeof createPurchaseSchema>
