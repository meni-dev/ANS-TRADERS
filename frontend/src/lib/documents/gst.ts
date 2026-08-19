/**
 * Client-side mirror of the server's `GstCalculator`, used only to show live totals while the user
 * is still typing. The server recomputes every figure on submit and its answer is the one that gets
 * stored — this exists so the counter can see the bill total before committing to it, not as a
 * second source of truth. Any change here must be made in `GstCalculator.cs` as well.
 */

export type LineInput = {
  quantity: number
  rate: number
  discountPercent: number
  gstRate: number
}

export type LineAmounts = {
  grossAmount: number
  /** This line's slice of a bill-level discount, already off the taxable value. */
  billDiscountShare?: number
  discountAmount: number
  taxableAmount: number
  cgstAmount: number
  sgstAmount: number
  igstAmount: number
  lineTotal: number
}

export type DocumentAmounts = {
  subTotal: number
  discountAmount: number
  taxableAmount: number
  cgstAmount: number
  sgstAmount: number
  igstAmount: number
  totalTax: number
  roundOff: number
  grandTotal: number
}

function round(value: number): number {
  // Scaling before rounding keeps 1.005 from landing on 1.00 the way toFixed does on binary floats.
  return Math.round((value + Number.EPSILON) * 100) / 100
}

export function computeLine(
  line: LineInput,
  isInterState: boolean,
  /** This line's slice of a bill-level discount, taken off the taxable value before tax. */
  billDiscountShare = 0,
): LineAmounts {
  const quantity = Number(line.quantity) || 0
  const rate = Number(line.rate) || 0
  const discountPercent = Number(line.discountPercent) || 0
  const gstRate = Number(line.gstRate) || 0

  const grossAmount = round(quantity * rate)
  const discountAmount = round((grossAmount * discountPercent) / 100)
  const share = round(billDiscountShare)
  const taxableAmount = round(grossAmount - discountAmount - share)
  const tax = round((taxableAmount * gstRate) / 100)

  // The remainder goes to SGST rather than rounding both halves — see the note in GstCalculator.cs.
  const cgstAmount = isInterState ? 0 : round(tax / 2)
  const sgstAmount = isInterState ? 0 : round(tax - cgstAmount)
  const igstAmount = isInterState ? tax : 0

  return {
    grossAmount,
    billDiscountShare: share,
    discountAmount,
    taxableAmount,
    cgstAmount,
    sgstAmount,
    igstAmount,
    lineTotal: round(taxableAmount + tax),
  }
}

export function computeDocument(lines: LineAmounts[]): DocumentAmounts {
  const sum = (pick: (line: LineAmounts) => number) => round(lines.reduce((total, l) => total + pick(l), 0))

  const taxableAmount = sum((l) => l.taxableAmount)
  const cgstAmount = sum((l) => l.cgstAmount)
  const sgstAmount = sum((l) => l.sgstAmount)
  const igstAmount = sum((l) => l.igstAmount)
  const totalTax = round(cgstAmount + sgstAmount + igstAmount)

  const beforeRounding = round(taxableAmount + totalTax)
  const grandTotal = Math.round(beforeRounding)

  return {
    subTotal: sum((l) => l.grossAmount),
    discountAmount: round(sum((l) => l.discountAmount) + sum((l) => l.billDiscountShare ?? 0)),
    taxableAmount,
    cgstAmount,
    sgstAmount,
    igstAmount,
    totalTax,
    roundOff: round(grandTotal - beforeRounding),
    grandTotal,
  }
}

/**
 * A supply is inter-state when the two parties sit in different GST states. An unknown state code
 * on the other party is treated as local — the same default the server applies.
 */
export function isInterState(sellerStateCode?: string | null, partyStateCode?: string | null): boolean {
  if (!sellerStateCode?.trim() || !partyStateCode?.trim()) return false
  return sellerStateCode.trim() !== partyStateCode.trim()
}

/**
 * Splits a bill-level discount across lines in proportion to what each contributes, and recomputes
 * their tax. Mirrors `GstCalculator.ApplyBillDiscount` — the server is still the authority, this is
 * so the counter sees the same total before saving as after.
 */
export function applyBillDiscount(
  lines: LineInput[],
  billDiscount: number,
  isInterState: boolean,
): LineAmounts[] {
  const computed = lines.map((l) => computeLine(l, isInterState))
  const discount = round(billDiscount)
  const totalTaxable = round(computed.reduce((t, l) => t + l.taxableAmount, 0))

  if (discount <= 0 || totalTaxable <= 0) return computed

  const shares = computed.map((l) => round((discount * l.taxableAmount) / totalTaxable))

  // The rounding remainder goes to the largest line, so the shares add to exactly the discount the
  // bill prints — see the note on the server side.
  let largest = 0
  computed.forEach((l, i) => {
    if (l.taxableAmount > computed[largest].taxableAmount) largest = i
  })
  shares[largest] = round(shares[largest] + discount - round(shares.reduce((t, s) => t + s, 0)))

  return lines.map((l, i) => computeLine(l, isInterState, shares[i]))
}
