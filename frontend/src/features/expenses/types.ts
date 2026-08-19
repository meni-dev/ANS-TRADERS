import { z } from 'zod'

/** Mirrors `Domain.Enums.ExpenseCategory`. Short on purpose — a long list stops being chosen from. */
export const EXPENSE_CATEGORIES = [
  { value: 'Rent', label: 'Rent' },
  { value: 'Salary', label: 'Salary and wages' },
  { value: 'Utilities', label: 'Electricity, water, phone' },
  { value: 'Freight', label: 'Freight and transport' },
  { value: 'ShopExpenses', label: 'Shop expenses' },
  { value: 'BankCharges', label: 'Bank charges and interest' },
  { value: 'Marketing', label: 'Advertising' },
  { value: 'TaxesAndFees', label: 'Taxes, licences and fees' },
  { value: 'Repairs', label: 'Repairs and maintenance' },
  { value: 'Other', label: 'Other' },
] as const

export type ExpenseCategory = (typeof EXPENSE_CATEGORIES)[number]['value']

/** Credit is absent: an expense that tendered nothing did not happen. */
export const EXPENSE_MODES = [
  { value: 'Cash', label: 'Cash' },
  { value: 'Upi', label: 'UPI' },
  { value: 'Card', label: 'Card' },
  { value: 'BankTransfer', label: 'Bank Transfer' },
  { value: 'Cheque', label: 'Cheque' },
] as const

export type ExpenseDto = {
  id: string
  expenseNumber: string
  expenseDate: string
  category: ExpenseCategory
  categoryLabel: string
  amount: number
  mode: string
  referenceNumber?: string | null
  paidTo?: string | null
  notes?: string | null
  isCancelled: boolean
  createdAt: string
}

export type ExpenseCategoryTotalDto = {
  category: string
  categoryLabel: string
  amount: number
  count: number
}

export type ExpenseSummaryDto = {
  total: number
  count: number
  byCategory: ExpenseCategoryTotalDto[]
}

export type ProfitAndLossDto = {
  fromDate: string
  toDate: string
  revenue: number
  costOfGoods: number
  grossProfit: number
  expenses: number
  netProfit: number
  costedLines: number
  uncostedLines: number
  /** How much of the period's sale lines carried a known cost. */
  costCoveragePercent: number
  /** False when some lines predate cost capture — the figure must then be shown with its coverage. */
  isComplete: boolean
  expensesByCategory: ExpenseCategoryTotalDto[]
}

export const createExpenseSchema = z.object({
  expenseDate: z.string().min(1, 'Pick a date'),
  category: z.string().min(1, 'What did the money go on?'),
  amount: z.number().positive('Enter what was spent'),
  mode: z.string().min(1, 'How was it paid?'),
  referenceNumber: z.string().optional(),
  paidTo: z.string().optional(),
  notes: z.string().optional(),
})

export type CreateExpenseFormValues = z.infer<typeof createExpenseSchema>
