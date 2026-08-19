import { z } from 'zod'

/** Mirrors `Domain.Enums.InvoiceTemplate`. */
export type InvoiceTemplateId = 'Classic' | 'Detailed' | 'Modern' | 'Traditional' | 'Minimal'

export type ShopSettingsDto = {
  name: string
  legalName?: string | null
  gstin?: string | null
  stateCode: string
  state: string
  addressLine1?: string | null
  addressLine2?: string | null
  city?: string | null
  pincode?: string | null
  phone?: string | null
  email?: string | null
  invoiceFooter?: string | null
  bankDetails?: string | null
  invoiceTerms?: string | null
  invoiceTemplate: InvoiceTemplateId
  /** `null` means the books are open. Everything on or before this date refuses to change. */
  booksLockedUpTo: string | null
}

// Patterns mirror the server-side rules in PartyRules.cs, so the shop's own details are held to
// the same standard as its customers' and the user never gets past the client only to be rejected.
const GSTIN_PATTERN = /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$/
const PINCODE_PATTERN = /^[0-9]{6}$/

const optional = (schema: z.ZodString) => schema.optional().or(z.literal(''))

export const shopSettingsSchema = z.object({
  name: z.string().min(1, 'The shop name is printed on every bill').max(200),
  legalName: optional(z.string().max(200)),
  gstin: optional(z.string().regex(GSTIN_PATTERN, 'Enter a valid 15-character GSTIN')),
  stateCode: z
    .string()
    .min(1, 'The state code decides IGST against CGST + SGST')
    .regex(/^[0-9]{2}$/, 'State code must be 2 digits'),
  state: z.string().min(1, 'State is required').max(100),
  addressLine1: optional(z.string().max(200)),
  addressLine2: optional(z.string().max(200)),
  city: optional(z.string().max(100)),
  pincode: optional(z.string().regex(PINCODE_PATTERN, 'Pincode must be 6 digits')),
  phone: optional(z.string().max(20)),
  email: optional(z.string().email('Enter a valid email address').max(200)),
  invoiceFooter: optional(z.string().max(500)),
  bankDetails: optional(z.string().max(500)),
  invoiceTerms: optional(z.string().max(1000)),
  invoiceTemplate: z.string().min(1, 'Pick an invoice template'),
})

export type ShopSettingsFormValues = z.infer<typeof shopSettingsSchema>
