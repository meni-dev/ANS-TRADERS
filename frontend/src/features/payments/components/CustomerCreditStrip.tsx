import { formatCurrency, formatDate } from '@/lib/format'
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutlineOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined'
import { Alert, Stack, Typography } from '@mui/material'
import { useCustomerAccountSummary } from '../hooks'

type CustomerCreditStripProps = {
  customerId: string | undefined
  /** The bill being written right now, so the limit is tested against the total it will reach. */
  currentBillTotal: number
}

/**
 * What the counter needs to know about a customer before handing over goods. It never blocks the
 * sale — who gets credit is the owner's call, and a screen that refuses him is a screen he works
 * around. It only makes sure he is not deciding blind.
 */
export function CustomerCreditStrip({ customerId, currentBillTotal }: CustomerCreditStripProps) {
  const { data } = useCustomerAccountSummary(customerId)

  if (!customerId || !data) return null

  const projected = data.outstandingBalance + currentBillTotal

  // Zero means nobody has set a limit, not that this customer may have none. Every customer starts
  // at zero, so treating it as a real limit would fire this warning on every bill in the shop — and
  // a warning that always fires is ignored inside a week.
  const hasLimit = data.creditLimit > 0
  const overLimit = hasLimit && projected > data.creditLimit

  // Ninety days is where a bounce stops saying anything useful about today's customer.
  const bouncedRecently = Boolean(data.lastBounceDate)

  return (
    <Stack spacing={1} sx={{ mt: 1.5 }}>
      <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
        Outstanding {formatCurrency(data.outstandingBalance)}
        {hasLimit ? ` · Limit ${formatCurrency(data.creditLimit)}` : ' · No credit limit set'}
        {data.advanceAmount > 0 ? ` · ${formatCurrency(data.advanceAmount)} on account` : ''}
        {data.pendingChequeAmount > 0
          ? ` · ${formatCurrency(data.pendingChequeAmount)} in uncleared cheques`
          : ''}
      </Typography>

      {bouncedRecently ? (
        <Alert severity="error" icon={<ErrorOutlineIcon fontSize="small" />} sx={{ py: 0.25 }}>
          Cheque {data.lastBounceChequeNumber} was returned on {formatDate(data.lastBounceDate)} —
          take cash.
        </Alert>
      ) : null}

      {data.overdueAmount > 0 ? (
        <Alert severity="warning" icon={<WarningAmberOutlinedIcon fontSize="small" />} sx={{ py: 0.25 }}>
          {formatCurrency(data.overdueAmount)} unpaid since {formatDate(data.oldestUnpaidDate)} — over
          60 days past due.
        </Alert>
      ) : null}

      {overLimit ? (
        <Alert severity="warning" icon={<InfoOutlinedIcon fontSize="small" />} sx={{ py: 0.25 }}>
          This bill takes them to {formatCurrency(projected)} against a{' '}
          {formatCurrency(data.creditLimit)} limit.
        </Alert>
      ) : null}
    </Stack>
  )
}
