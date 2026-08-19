import { z } from 'zod'

export type SupplierDto = {
  id: string
  name: string
  phone: string
  email?: string | null
  gstin?: string | null
  contactPerson?: string | null
  addressLine1?: string | null
  addressLine2?: string | null
  city?: string | null
  state?: string | null
  stateCode?: string | null
  pincode?: string | null
  paymentTerms?: string | null
  openingBalance: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

// Patterns mirror the server-side rules in PartyRules.cs. Keeping them identical means the user
// never gets past the client only to be rejected by the API.
const GSTIN_PATTERN = /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$/
const PHONE_PATTERN = /^[0-9]{10,15}$/
const PINCODE_PATTERN = /^[0-9]{6}$/

const optional = (schema: z.ZodString) => schema.optional().or(z.literal(''))

const baseSupplierFields = {
  name: z.string().min(1, 'Name is required').max(200),
  phone: z
    .string()
    .min(1, 'Phone is required')
    .regex(PHONE_PATTERN, 'Enter a valid phone number (10-15 digits)'),
  email: optional(z.string().email('Enter a valid email address').max(200)),
  gstin: optional(z.string().regex(GSTIN_PATTERN, 'Enter a valid 15-character GSTIN')),
  contactPerson: optional(z.string().max(200)),
  addressLine1: optional(z.string().max(200)),
  addressLine2: optional(z.string().max(200)),
  city: optional(z.string().max(100)),
  state: optional(z.string().max(100)),
  stateCode: optional(z.string().regex(/^[0-9]{2}$/, 'State code must be 2 digits')),
  pincode: optional(z.string().regex(PINCODE_PATTERN, 'Pincode must be 6 digits')),
  paymentTerms: optional(z.string().max(100)),
}

export const createSupplierSchema = z.object({
  ...baseSupplierFields,
  openingBalance: z.number('Enter an opening balance').min(0, 'Cannot be negative'),
})

export const editSupplierSchema = z.object({
  ...baseSupplierFields,
  isActive: z.boolean(),
})

export type CreateSupplierFormValues = z.infer<typeof createSupplierSchema>
export type EditSupplierFormValues = z.infer<typeof editSupplierSchema>
