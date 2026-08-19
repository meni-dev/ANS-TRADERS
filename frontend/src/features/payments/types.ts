import { z } from 'zod'

/** Mirrors `Domain.Enums.PaymentDirection`. */
export type PaymentDirection = 'Received' | 'Paid'

/**
 * Mirrors `Domain.Enums.PaymentStatus`.
 *
 * `Pending` is only ever a post-dated cheque: recorded and visible, but it has moved no balance and
 * settled no bill. It becomes `Posted` when somebody banks it.
 */
export type PaymentStatus = 'Pending' | 'Posted' | 'Reversed'

/** Mirrors `Domain.Enums.ChequeStatus`. */
export type ChequeStatus = 'Pending' | 'Deposited' | 'Cleared' | 'Bounced' | 'Cancelled'

/** Mirrors `Domain.Enums.PartyLedgerEntryType`. */
export type PartyLedgerEntryType =
  | 'Opening'
  | 'Invoice'
  | 'InvoiceCancelled'
  | 'PurchaseBill'
  | 'PurchaseCancelled'
  | 'PaymentReceived'
  | 'PaymentMade'
  | 'PaymentCancelled'
  | 'ChequeBounced'
  | 'ChequeBounceCharge'
  | 'Adjustment'

/** How each entry type reads on a statement, in the words a shopkeeper uses. */
export const LEDGER_ENTRY_LABELS: Record<PartyLedgerEntryType, string> = {
  Opening: 'Opening balance',
  Invoice: 'Sales invoice',
  InvoiceCancelled: 'Invoice cancelled',
  PurchaseBill: 'Purchase bill',
  PurchaseCancelled: 'Purchase cancelled',
  PaymentReceived: 'Receipt',
  PaymentMade: 'Payment made',
  PaymentCancelled: 'Payment cancelled',
  ChequeBounced: 'Cheque returned',
  ChequeBounceCharge: 'Bank charge',
  Adjustment: 'Adjustment',
}

export type PaymentAllocationDto = {
  id: string
  invoiceId?: string | null
  purchaseId?: string | null
  documentNumber: string
  documentDate: string
  amount: number
  isReversed: boolean
}

export type ChequeDto = {
  chequeNumber: string
  bankName: string
  /** The date written on the cheque. Later than today means it cannot be banked yet. */
  chequeDate: string
  receivedOn: string
  status: ChequeStatus
  depositedOn?: string | null
  clearedOn?: string | null
  bouncedOn?: string | null
  bounceReason?: string | null
  /** Which statuses it may still move to — this is what drives the register's row actions. */
  nextStatuses: ChequeStatus[]
}

export type PaymentDto = {
  id: string
  /** Null for money taken at the counter: the invoice the customer was handed is the receipt. */
  receiptNumber?: string | null
  direction: PaymentDirection
  paymentDate: string
  customerId?: string | null
  supplierId?: string | null
  partyName: string
  amount: number
  allocatedAmount: number
  /** Money on account against no bill — spendable later. */
  unallocatedAmount: number
  mode: string
  referenceNumber?: string | null
  notes?: string | null
  status: PaymentStatus
  isCounterPayment: boolean
  cheque?: ChequeDto | null
  allocations: PaymentAllocationDto[]
  createdAt: string
}

export type PaymentListItemDto = {
  id: string
  receiptNumber?: string | null
  direction: PaymentDirection
  paymentDate: string
  partyName: string
  amount: number
  unallocatedAmount: number
  mode: string
  status: PaymentStatus
  isCounterPayment: boolean
  chequeNumber?: string | null
  chequeStatus?: ChequeStatus | null
  chequeDate?: string | null
}

export type PaymentSummaryDto = {
  collected: number
  paidOut: number
  netCash: number
  /** Cheques taken but not yet cleared. Deliberately not added to `collected`. */
  chequesInHand: number
  chequesInHandCount: number
  paymentCount: number
}

export type DuesSummaryDto = {
  totalReceivable: number
  totalPayable: number
  advancesHeld: number
  customersWithDues: number
  suppliersWithDues: number
}

export type PartyLedgerEntryDto = {
  id: string
  entryType: PartyLedgerEntryType
  /** Signed: positive increases what is open on the account. */
  amount: number
  balanceAfter: number
  entryDate: string
  referenceId?: string | null
  referenceNumber?: string | null
  notes?: string | null
}

export type PartyStatementDto = {
  partyId: string
  partyName: string
  openingBalance: number
  closingBalance: number
  fromDate?: string | null
  toDate?: string | null
  entries: PartyLedgerEntryDto[]
  totalCount: number
  page: number
  pageSize: number
}

export type OpenDocumentDto = {
  id: string
  documentNumber: string
  documentDate: string
  dueDate?: string | null
  grandTotal: number
  amountPaid: number
  balanceDue: number
  /** Days past the due date, not days since billing. */
  daysOld: number
}

export type CustomerAccountSummaryDto = {
  customerId: string
  outstandingBalance: number
  /** Zero means no limit was ever set — never treat it as a limit of zero. */
  creditLimit: number
  creditDays: number
  advanceAmount: number
  pendingChequeAmount: number
  /** Open more than 60 days past due — the figure worth acting on. */
  overdueAmount: number
  oldestUnpaidDate?: string | null
  lastBounceDate?: string | null
  lastBounceChequeNumber?: string | null
}

/**
 * Cheque fields, required only when the tender is a cheque. Kept as its own schema so both the
 * receipt form and the billing form can drop it in.
 */
export const chequeSchema = z.object({
  chequeNumber: z.string().trim().min(1, 'Cheque number is needed'),
  bankName: z.string().trim().min(1, "Which bank is it drawn on?"),
  chequeDate: z.string().min(1, 'Date on the cheque'),
  receivedOn: z.string().min(1, 'When was it handed over?'),
})

export const recordPaymentSchema = z
  .object({
    direction: z.enum(['Received', 'Paid']),
    customerId: z.string().optional(),
    supplierId: z.string().optional(),
    paymentDate: z.string().min(1, 'Pick a date'),
    amount: z.number().positive('Enter how much changed hands'),
    mode: z.string().min(1, 'How did the money arrive?'),
    referenceNumber: z.string().optional(),
    notes: z.string().optional(),
    cheque: chequeSchema.optional(),
    /** Empty means settle the party's open bills oldest first. */
    allocations: z.array(z.object({ documentId: z.string(), amount: z.number() })),
    autoAllocateOldestFirst: z.boolean(),
  })
  .superRefine((values, ctx) => {
    const party = values.direction === 'Received' ? values.customerId : values.supplierId
    if (!party) {
      ctx.addIssue({
        code: 'custom',
        path: [values.direction === 'Received' ? 'customerId' : 'supplierId'],
        message: 'Pick who the money is from',
      })
    }

    // Credit is not a tender. Choosing it here would record a payment in which nothing moved.
    if (values.mode === 'Credit') {
      ctx.addIssue({ code: 'custom', path: ['mode'], message: 'Credit is not a way of paying' })
    }

    if (values.mode === 'Cheque' && !values.cheque) {
      ctx.addIssue({ code: 'custom', path: ['cheque'], message: 'Cheque details are needed' })
    }
  })

export type RecordPaymentFormValues = z.infer<typeof recordPaymentSchema>

export const bounceChequeSchema = z.object({
  bouncedOn: z.string().min(1, 'When did the bank return it?'),
  reason: z.string().trim().min(1, 'What did the bank say?'),
  /** Optional and defaulting to nothing — never to a stored figure the shop forgot it set. */
  chargeAmount: z.number().min(0).optional(),
})

export type BounceChequeFormValues = z.infer<typeof bounceChequeSchema>
