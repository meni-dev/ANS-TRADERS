export type CashPositionDto = {
  date: string
  openingCash: number
  cashReceived: number
  cashPaidOut: number
  cashExpenses: number
  /** Opening + received − paid out − expenses. What should be in the drawer. */
  expectedCash: number
  /** True when the previous day was never closed, so the opening was computed rather than counted. */
  openingIsCarriedForward: boolean
  isClosed: boolean
  countedCash?: number | null
  difference?: number | null
  reason?: string | null
}

export type DayCloseDto = {
  id: string
  closeDate: string
  openingCash: number
  cashReceived: number
  cashPaidOut: number
  cashExpenses: number
  expectedCash: number
  countedCash: number
  difference: number
  reason?: string | null
  notes?: string | null
  createdAt: string
}

export type CashBookEntryDto = {
  date: string
  kind: 'Receipt' | 'Paid out' | 'Expense' | 'Day close'
  reference: string
  particulars: string
  in: number
  out: number
  balance: number
}

export type CashBookDto = {
  fromDate: string
  toDate: string
  openingBalance: number
  closingBalance: number
  entries: CashBookEntryDto[]
}
/** Mirrors `Domain.Enums.MoneyMovementKind`. */
export type MoneyMovementKind =
  | 'OpeningFloat'
  | 'BankToCash'
  | 'CashToBank'
  | 'CapitalIntroduced'
  | 'Drawings'
  | 'OpeningStock'

export type MoneyMovement = {
  id: string
  movementDate: string
  kind: MoneyMovementKind
  kindLabel: string
  amount: number
  /** False when the money never passed through the till — straight into the bank. */
  affectsCash: boolean
  referenceNumber: string | null
  notes: string | null
  isCancelled: boolean
  createdByName: string | null
}

export type CapitalSummary = {
  openingFloat: number
  openingStockValue: number
  capitalIntroduced: number
  drawings: number
  bankToCash: number
  cashToBank: number
  netInvested: number
}

/**
 * What the owner can record by hand. Opening stock is left out on purpose — it is written when a
 * part is set up with stock, and typing a second one would double what the shop thinks it put in.
 */
export const MONEY_MOVEMENTS: {
  value: Exclude<MoneyMovementKind, 'OpeningStock'>
  label: string
  hint: string
}[] = [
  { value: 'OpeningFloat', label: 'Opening float', hint: 'The cash already in the drawer on day one. Once only' },
  { value: 'BankToCash', label: 'Drawn from bank', hint: 'Money taken out of the bank into the till' },
  { value: 'CashToBank', label: 'Banked', hint: 'Money from the till paid into the bank' },
  { value: 'CapitalIntroduced', label: 'Capital introduced', hint: "The owner's own money going in" },
  { value: 'Drawings', label: 'Drawings', hint: 'The owner taking money out. Not an expense' },
]
