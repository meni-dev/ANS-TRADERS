import { z } from 'zod'

export type ProductStockDto = {
  id: string
  partNumber: string
  itemName: string
  vehicleBrand?: string | null
  vehicleModel?: string | null
  uqc: string
  openingStock: number
  stockOnHand: number
  reorderLevel: number
  /** Stock held at the item's purchase rate — what the shelf is worth to the shop. */
  stockValue: number
  isActive: boolean
}

/** Mirrors `Domain.Enums.StockMovementType`. */
export type StockMovementType =
  | 'Opening'
  | 'Purchase'
  | 'Sale'
  | 'PurchaseCancelled'
  | 'SaleCancelled'
  | 'Adjustment'

export type StockMovementDto = {
  id: string
  productId: string
  partNumber: string
  itemName: string
  movementType: StockMovementType
  /** Signed: positive brought stock in, negative took it out. */
  quantity: number
  balanceAfter: number
  movedAt: string
  referenceId?: string | null
  referenceNumber?: string | null
  notes?: string | null
}

export type StockSummaryDto = {
  totalItems: number
  lowStockCount: number
  outOfStockCount: number
  totalStockValue: number
}

/** Labels for the ledger filter and the movement column, keyed by what the API returns. */
export const MOVEMENT_TYPE_LABELS: Record<StockMovementType, string> = {
  Opening: 'Opening',
  Purchase: 'Purchase',
  Sale: 'Sale',
  PurchaseCancelled: 'Purchase cancelled',
  SaleCancelled: 'Sale cancelled',
  Adjustment: 'Adjustment',
}

export const adjustStockSchema = z.object({
  productId: z.string().min(1, 'Pick a product'),
  countedQuantity: z.number('Enter the counted quantity').min(0, 'Cannot be negative'),
  reason: z.string().min(1, 'Pick why the count is being corrected'),
  notes: z.string().max(500).optional(),
})

export type AdjustStockFormValues = z.infer<typeof adjustStockSchema>

/**
 * Mirrors `Domain.Enums.StockAdjustmentReason`. Coded so the loss report can add them up — a
 * sentence explains one correction, a code counts a year of them.
 */
export const ADJUSTMENT_REASONS = [
  { value: 'CountingError', label: 'Counting error', hint: 'The book was wrong, not the shelf' },
  { value: 'Damage', label: 'Damaged', hint: 'Broken, rusted, packaging destroyed' },
  { value: 'Expiry', label: 'Expired', hint: 'Past its shelf life' },
  { value: 'TheftOrMissing', label: 'Missing or taken', hint: 'Gone with no explanation' },
  { value: 'FreeIssue', label: 'Given free', hint: 'Goodwill, sample, warranty fit' },
  { value: 'Scrapped', label: 'Scrapped', hint: 'Sold for scrap or written off' },
  { value: 'Other', label: 'Other', hint: '' },
] as const

export type DeadStockRow = {
  productId: string
  partNumber: string
  itemName: string
  vehicleBrand: string | null
  stockOnHand: number
  purchaseRate: number
  valueAtCost: number
  /** Null when the part has never been sold — worse than "not lately", and shown differently. */
  lastSoldOn: string | null
  daysSinceLastSale: number | null
}

export type DeadStockReport = {
  monthsWithoutSale: number
  asOf: string
  totalValue: number
  neverSoldCount: number
  neverSoldValue: number
  rows: DeadStockRow[]
}

export type RateDriftRow = {
  productId: string
  partNumber: string
  itemName: string
  stockOnHand: number
  lastPurchaseRate: number
  lastPurchasedOn: string | null
  cataloguePurchaseRate: number
  sellingRate: number
  mrp: number
  /** Null when the part has no selling price at all. Not the same as a margin of zero. */
  marginPercent: number | null
  sellingBelowCost: boolean
  sellingRateMissing: boolean
}

export type RateDriftReport = {
  marginFloorPercent: number
  belowCostCount: number
  thinMarginCount: number
  unpricedCount: number
  rows: RateDriftRow[]
}

export type ReorderRow = {
  productId: string
  partNumber: string
  itemName: string
  stockOnHand: number
  reorderLevel: number
  dailyVelocity: number
  /** Null when nothing is moving — a part nobody buys has no date at which it runs out. */
  daysOfCover: number | null
  suggestedQuantity: number
  lastPurchaseRate: number
  suggestedValue: number
}

export type ReorderReport = {
  windowDays: number
  coverDays: number
  totalSuggestedValue: number
  outOfStockCount: number
  rows: ReorderRow[]
}
