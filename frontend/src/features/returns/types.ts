import { z } from 'zod'

/** Mirrors `Domain.Enums.CreditNoteStatus` / `DebitNoteStatus`. */
export type ReturnNoteStatus = 'Issued' | 'Cancelled'

export type ReturnNoteItemDto = {
  id: string
  /** The invoice or purchase line this reverses. */
  documentItemId: string
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

export type CreditNoteDto = {
  id: string
  creditNoteNumber: string
  financialYear: string
  noteDate: string
  invoiceId: string
  invoiceNumber: string
  invoiceDate: string
  customerId?: string | null
  customerName: string
  customerPhone?: string | null
  customerGstin?: string | null
  customerStateCode?: string | null
  isInterState: boolean
  itemCount: number
  reason: string
  subTotal: number
  discountAmount: number
  taxableAmount: number
  cgstAmount: number
  sgstAmount: number
  igstAmount: number
  totalTax: number
  roundOff: number
  grandTotal: number
  /** How much went against the bill. The rest is credit on the account. */
  appliedToInvoiceAmount: number
  refundedAmount: number
  /** What can still be handed back in cash — never more than the shop actually took. */
  refundableAmount: number
  status: ReturnNoteStatus
  items: ReturnNoteItemDto[]
  createdAt: string
}

export type CreditNoteListItemDto = {
  id: string
  creditNoteNumber: string
  noteDate: string
  invoiceNumber: string
  customerName: string
  itemCount: number
  grandTotal: number
  refundedAmount: number
  status: ReturnNoteStatus
}

export type ReturnableLineDto = {
  documentItemId: string
  productId: string
  partNumber: string
  itemName: string
  uqc: string
  quantitySold: number
  quantityReturned: number
  /** Sold less already returned. The only number the form may exceed nothing above. */
  quantityReturnable: number
  rate: number
  discountPercent: number
  gstRate: number
}

export type ReturnableDocumentDto = {
  documentId: string
  documentNumber: string
  documentDate: string
  partyName: string
  /** From the original document — the preview splits tax the same way the bill did. */
  isInterState: boolean
  canReturn: boolean
  /** Shown instead of a dead form when nothing can come back. */
  blockedReason?: string | null
  lines: ReturnableLineDto[]
}

/** The purchase side is the same shape with the nouns swapped, as it is on the server. */
export type DebitNoteDto = Omit<
  CreditNoteDto,
  | 'creditNoteNumber'
  | 'invoiceId'
  | 'invoiceNumber'
  | 'invoiceDate'
  | 'customerId'
  | 'customerName'
  | 'customerPhone'
  | 'customerGstin'
  | 'customerStateCode'
  | 'appliedToInvoiceAmount'
> & {
  debitNoteNumber: string
  purchaseId: string
  purchaseNumber: string
  purchaseDate: string
  supplierId: string
  supplierName: string
  supplierGstin?: string | null
  supplierStateCode?: string | null
  appliedToPurchaseAmount: number
}

export type DebitNoteListItemDto = {
  id: string
  debitNoteNumber: string
  noteDate: string
  purchaseNumber: string
  supplierName: string
  itemCount: number
  grandTotal: number
  refundedAmount: number
  status: ReturnNoteStatus
}

/**
 * Quantity is the only figure the form sends. Rate, discount and GST rate come from the original
 * line on the server — a credit note has to reverse the tax that was actually charged, and letting
 * the client supply a rate is exactly how it would stop doing that.
 */
export const returnLineSchema = z.object({
  documentItemId: z.string(),
  quantity: z.number().min(0),
  /** Carried for display and for the live total only; never sent. */
  itemName: z.string(),
  uqc: z.string(),
  returnable: z.number(),
  rate: z.number(),
  discountPercent: z.number(),
  gstRate: z.number(),
})

export const createReturnSchema = z
  .object({
    noteDate: z.string().min(1, 'Pick a date'),
    reason: z.string().trim().min(1, 'Say why the goods came back').max(500),
    lines: z.array(returnLineSchema),
    refundNow: z.boolean(),
    refundAmount: z.number().min(0),
    refundMode: z.string(),
    refundReference: z.string().optional(),
  })
  .superRefine((values, ctx) => {
    if (!values.lines.some((l) => l.quantity > 0)) {
      ctx.addIssue({ code: 'custom', path: ['lines'], message: 'Enter how much is coming back' })
    }

    values.lines.forEach((line, index) => {
      if (line.quantity > line.returnable) {
        ctx.addIssue({
          code: 'custom',
          path: ['lines', index, 'quantity'],
          message: `Only ${line.returnable} can still be returned`,
        })
      }
    })
  })

export type CreateReturnFormValues = z.infer<typeof createReturnSchema>
