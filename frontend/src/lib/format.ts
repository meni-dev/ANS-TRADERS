const currencyFormatter = new Intl.NumberFormat('en-IN', {
  style: 'currency',
  currency: 'INR',
  maximumFractionDigits: 2,
})

export function formatCurrency(value: number): string {
  return currencyFormatter.format(value)
}

const dateFormatter = new Intl.DateTimeFormat('en-IN', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
})

/**
 * Renders an ISO date (`2026-08-16`) or timestamp as `16 Aug 2026`. Documents come back from the
 * API as bare dates, which `new Date()` reads as UTC midnight — parsing the parts by hand avoids
 * the bill sliding to the previous day for anyone west of Greenwich.
 */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '—'

  const [datePart] = value.split('T')
  const [year, month, day] = datePart.split('-').map(Number)

  if (!year || !month || !day) return value

  return dateFormatter.format(new Date(year, month - 1, day))
}

/** Today as `YYYY-MM-DD` in the user's own timezone, ready for a date input. */
export function todayIso(): string {
  const now = new Date()
  const month = `${now.getMonth() + 1}`.padStart(2, '0')
  const day = `${now.getDate()}`.padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

/** Drops trailing zeros so a whole-number quantity reads as `10`, not `10.000`. */
export function formatQuantity(value: number): string {
  return Number(value).toLocaleString('en-IN', { maximumFractionDigits: 3 })
}
