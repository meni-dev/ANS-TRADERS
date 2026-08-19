export type DashboardTodayDto = {
  salesTotal: number
  invoiceCount: number
  purchaseTotal: number
  purchaseCount: number
}

export type DashboardMonthDto = {
  salesTotal: number
  invoiceCount: number
  purchaseTotal: number
  lastMonthSalesTotal: number
  /** Null when last month had no sales — a jump from nothing is not a percentage. */
  changePercent: number | null
}

export type MoneyPositionDto = {
  receivable: number
  receivableInvoiceCount: number
  customersWithDues: number
  /** Billed but inside the customer's credit period. This is what stops every rupee reading late. */
  receivableNotDue: number
  receivableCurrent: number
  receivable31To60: number
  receivableOver60: number
  payable: number
  payableBillCount: number
  suppliersWithDues: number
  /** Money held against no bill. Reported apart, never netted off what others owe. */
  advancesHeld: number
}

/** One HSN code's contribution to the month, in the shape GSTR-1 Table 12 asks for. */
export type HsnSummaryRowDto = {
  hsn: string
  uqc: string
  quantity: number
  taxableValue: number
  cgstAmount: number
  sgstAmount: number
  igstAmount: number
  totalTax: number
}

export type GstSummaryDto = {
  outputTaxable: number
  outputCgst: number
  outputSgst: number
  outputIgst: number
  outputTotal: number
  inputTaxable: number
  inputCgst: number
  inputSgst: number
  inputIgst: number
  inputTotal: number
  /** Negative means input credit exceeded output tax and is carried forward. */
  netPayable: number
  hsn: HsnSummaryRowDto[]
}

export type AuditChecksDto = {
  financialYear: string
  missingInvoiceNumbers: string[]
  missingInvoiceCount: number
  missingPurchaseNumbers: string[]
  missingPurchaseCount: number
  cancelledInvoiceCount: number
  cancelledPurchaseCount: number
  stockAdjustmentCount: number
  stockAdjustmentNetQuantity: number
  b2BInvoiceCount: number
  b2BSales: number
  b2CInvoiceCount: number
  b2CSales: number
  highValueWithoutGstinCount: number
  highValueWithoutGstinThreshold: number
  /** Distinct products sold this month with no HSN on the master. */
  itemsSoldWithoutHsnCount: number
  salesWithoutHsn: number
  reconciliation: ReconciliationChecksDto
}

/**
 * Rows whose cached total no longer matches the entries behind it. All four must be zero; anything
 * else means a figure on screen has quietly stopped being backed by anything.
 */
export type ReconciliationChecksDto = {
  partyBalanceMismatches: number
  documentBalanceMismatches: number
  allocationMismatches: number
  stockMismatches: number
  totalMismatches: number
  isClean: boolean
}

export type SalesTrendPointDto = {
  date: string
  salesTotal: number
  invoiceCount: number
}

export type ReorderItemDto = {
  productId: string
  partNumber: string
  itemName: string
  uqc: string
  stockOnHand: number
  reorderLevel: number
}

export type TopSellingItemDto = {
  productId: string
  partNumber: string
  itemName: string
  uqc: string
  quantity: number
  salesValue: number
}

export type RecentInvoiceDto = {
  id: string
  invoiceNumber: string
  invoiceDate: string
  customerName: string
  grandTotal: number
  balanceDue: number
  status: string
}

export type DashboardDto = {
  asOf: string
  today: DashboardTodayDto
  month: DashboardMonthDto
  money: MoneyPositionDto
  /** Null when this person may not see the registers — the panel is hidden rather than blanked. */
  gst: GstSummaryDto | null
  audit: AuditChecksDto
  salesTrend: SalesTrendPointDto[]
  reorder: ReorderItemDto[]
  topSellers: TopSellingItemDto[]
  recentInvoices: RecentInvoiceDto[]
}
