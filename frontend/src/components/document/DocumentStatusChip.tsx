import { Chip } from '@mui/material'

/**
 * Status pill shared by the purchase and invoice screens. Cancelled is the only state worth
 * colouring loudly — Received and Issued are the normal case and would just add noise to every row.
 */
export function DocumentStatusChip({ status }: { status: string }) {
  const cancelled = status === 'Cancelled'

  return (
    <Chip
      label={status}
      size="small"
      sx={{
        bgcolor: cancelled ? 'error.light' : 'success.light',
        color: cancelled ? 'error.dark' : 'success.dark',
      }}
    />
  )
}

/** Paid / partial / unpaid pill, derived from what is still outstanding on a document. */
export function BalanceChip({ balanceDue, grandTotal }: { balanceDue: number; grandTotal: number }) {
  if (balanceDue <= 0) {
    return <Chip label="Paid" size="small" sx={{ bgcolor: 'success.light', color: 'success.dark' }} />
  }

  const partial = balanceDue < grandTotal

  return (
    <Chip
      label={partial ? 'Partial' : 'Unpaid'}
      size="small"
      sx={{
        bgcolor: partial ? 'warning.light' : 'grey.100',
        color: partial ? 'warning.dark' : 'text.secondary',
      }}
    />
  )
}
