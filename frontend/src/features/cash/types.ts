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
