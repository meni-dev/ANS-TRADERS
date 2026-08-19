import { z } from 'zod'

export type CustomerDto = {
  id: string
  name: string
  phone: string
  email?: string | null
  gstin?: string | null
  addressLine1?: string | null
  addressLine2?: string | null
  city?: string | null
  state?: string | null
  stateCode?: string | null
  pincode?: string | null
  creditLimit: number
  /** Days before a bill falls due. Zero means payment on delivery. */
  creditDays: number
  /** What they owe right now, straight off the party ledger. */
  outstandingBalance: number
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

const baseCustomerFields = {
  name: z.string().min(1, 'Name is required').max(200),
  phone: z
    .string()
    .min(1, 'Phone is required')
    .regex(PHONE_PATTERN, 'Enter a valid phone number (10-15 digits)'),
  email: optional(z.string().email('Enter a valid email address').max(200)),
  gstin: optional(z.string().regex(GSTIN_PATTERN, 'Enter a valid 15-character GSTIN')),
  addressLine1: optional(z.string().max(200)),
  addressLine2: optional(z.string().max(200)),
  city: optional(z.string().max(100)),
  state: optional(z.string().max(100)),
  stateCode: optional(z.string().regex(/^[0-9]{2}$/, 'State code must be 2 digits')),
  pincode: optional(z.string().regex(PINCODE_PATTERN, 'Pincode must be 6 digits')),
  creditLimit: z.number('Enter a credit limit').min(0, 'Cannot be negative'),
  creditDays: z
    .number('Enter the credit period in days')
    .int('Whole days only')
    .min(0, 'Cannot be negative')
    .max(180, 'More than six months is almost certainly a typo'),
}

export const createCustomerSchema = z.object({
  ...baseCustomerFields,
  openingBalance: z.number('Enter an opening balance').min(0, 'Cannot be negative'),
})

export const editCustomerSchema = z.object({
  ...baseCustomerFields,
  isActive: z.boolean(),
})

export type CreateCustomerFormValues = z.infer<typeof createCustomerSchema>
export type EditCustomerFormValues = z.infer<typeof editCustomerSchema>
