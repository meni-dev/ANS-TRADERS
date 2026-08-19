import type { DocumentAmounts } from '@/lib/documents/gst'
import { formatCurrency } from '@/lib/format'
import { Box, Divider, Stack, Typography } from '@mui/material'

type DocumentTotalsProps = {
  amounts: DocumentAmounts
  isInterState: boolean
  /** Shown under the grand total once a payment mode has been chosen. */
  amountPaid?: number
}

function Row({ label, value, muted }: { label: string; value: string; muted?: boolean }) {
  return (
    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
      <Typography sx={{ fontSize: 13, color: muted ? 'text.disabled' : 'text.secondary' }}>
        {label}
      </Typography>
      <Typography
        sx={{
          fontSize: 13.5,
          fontWeight: 500,
          fontVariantNumeric: 'tabular-nums',
          color: muted ? 'text.disabled' : 'text.primary',
        }}
      >
        {value}
      </Typography>
    </Stack>
  )
}

/**
 * The tax summary that closes every document. IGST and CGST+SGST are mutually exclusive by law, so
 * only the applicable pair is shown — printing both with one side zeroed is how bills get queried.
 */
export function DocumentTotals({ amounts, isInterState, amountPaid }: DocumentTotalsProps) {
  const balance = amountPaid === undefined ? undefined : amounts.grandTotal - amountPaid

  return (
    <Stack spacing={1}>
      <Row label="Sub total" value={formatCurrency(amounts.subTotal)} />

      {amounts.discountAmount > 0 && (
        <Row label="Discount" value={`− ${formatCurrency(amounts.discountAmount)}`} />
      )}

      <Row label="Taxable value" value={formatCurrency(amounts.taxableAmount)} />

      {isInterState ? (
        <Row label="IGST" value={formatCurrency(amounts.igstAmount)} />
      ) : (
        <>
          <Row label="CGST" value={formatCurrency(amounts.cgstAmount)} />
          <Row label="SGST" value={formatCurrency(amounts.sgstAmount)} />
        </>
      )}

      {amounts.roundOff !== 0 && (
        <Row
          label="Round off"
          value={`${amounts.roundOff > 0 ? '+' : '−'} ${formatCurrency(Math.abs(amounts.roundOff))}`}
          muted
        />
      )}

      <Divider sx={{ my: 0.5 }} />

      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2 }}>
        <Typography sx={{ fontSize: 13.5, fontWeight: 700 }}>Grand total</Typography>
        <Typography sx={{ fontSize: 20, fontWeight: 700, letterSpacing: '-0.02em', fontVariantNumeric: 'tabular-nums' }}>
          {formatCurrency(amounts.grandTotal)}
        </Typography>
      </Stack>

      {balance !== undefined && (
        <Box sx={{ pt: 0.5 }}>
          <Row label="Paid" value={formatCurrency(amountPaid ?? 0)} />
          <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'baseline', gap: 2, mt: 0.5 }}>
            <Typography sx={{ fontSize: 13, fontWeight: 600, color: balance > 0 ? 'warning.dark' : 'success.dark' }}>
              {balance > 0 ? 'Balance due' : 'Settled'}
            </Typography>
            <Typography
              sx={{
                fontSize: 14,
                fontWeight: 700,
                fontVariantNumeric: 'tabular-nums',
                color: balance > 0 ? 'warning.dark' : 'success.dark',
              }}
            >
              {formatCurrency(Math.max(balance, 0))}
            </Typography>
          </Stack>
        </Box>
      )}
    </Stack>
  )
}
