import { describeError } from '@/lib/api/errors'
import AccountBalanceWalletOutlinedIcon from '@mui/icons-material/AccountBalanceWalletOutlined'
import LoginOutlinedIcon from '@mui/icons-material/LoginOutlined'
import CallReceivedIcon from '@mui/icons-material/CallReceived'
import CallMadeIcon from '@mui/icons-material/CallMade'
import { StatTile } from '@/components/data/StatTile'
import { useAuth } from '@/features/auth/AuthProvider'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { formatCurrency, formatDate, todayIso } from '@/lib/format'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  Divider,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { useCashBook, useCashPosition, useCloseDay } from '../hooks'
import { MoneyMovementsCard } from './MoneyMovementsCard'

function monthStart(): string {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`
}

/**
 * The drawer: what should be in it, what was, and every movement between.
 * <p>
 * Cash only. UPI and card money is real but it sits in a bank — counting it here would make "the
 * till is short" an argument nobody can settle.
 * </p>
 */
export function CashPage() {
  const { notify } = useNotification()
  const { can } = useAuth()
  const [date, setDate] = useState(todayIso())
  const [counted, setCounted] = useState('')
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  const [fromDate, setFromDate] = useState(monthStart())
  const [toDate, setToDate] = useState(todayIso())

  const position = useCashPosition(date)
  const book = useCashBook({ fromDate, toDate })
  const close = useCloseDay()

  const p = position.data
  const difference = p && counted !== '' ? Number(counted) - p.expectedCash : 0
  const needsReason = difference !== 0 && counted !== ''

  async function submit() {
    if (!p) return
    setError(null)

    try {
      await close.mutateAsync({
        closeDate: p.date,
        countedCash: Number(counted),
        reason: reason.trim() || undefined,
      })
      notify(`${formatDate(p.date)} closed`, 'success')
      setCounted('')
      setReason('')
    } catch (caught) {
      setError(describeError(caught, 'Could not close the day'))
    }
  }

  return (
    <Stack spacing={2.5}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: { sm: 'flex-end' } }}>
        <Box sx={{ flex: 1 }}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            Cash &amp; Day Close
          </Typography>
          <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>
            What should be in the drawer, and what actually is
          </Typography>
        </Box>
        <TextField
          size="small"
          type="date"
          label="Day"
          value={date}
          onChange={(e) => setDate(e.target.value)}
        />
      </Stack>

      {error ? <Alert severity="error">{error}</Alert> : null}

      {p?.openingIsCarriedForward && !p.isClosed ? (
        <Alert severity="info">
          The previous day was never closed, so this opening figure was worked out from the entries
          rather than counted. Closing each day is what keeps it a fact instead of an inference.
        </Alert>
      ) : null}

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', lg: 'repeat(4, 1fr)' },
        }}
      >
        <StatTile
          label="Opened with"
          value={formatCurrency(p?.openingCash ?? 0)}
          loading={position.isLoading}
          icon={<LoginOutlinedIcon />}
          iconTone="blue"
          tinted
        />
        <StatTile
          label="Cash in"
          value={formatCurrency(p?.cashReceived ?? 0)}
          tone="success"
          loading={position.isLoading}
          icon={<CallReceivedIcon />}
          iconTone="teal"
          tinted
        />
        <StatTile
          label="Cash out"
          icon={<CallMadeIcon />}
          iconTone="rose"
          tinted
          value={formatCurrency((p?.cashPaidOut ?? 0) + (p?.cashExpenses ?? 0))}
          caption={p ? `${formatCurrency(p.cashExpenses)} of it expenses` : undefined}
          loading={position.isLoading}
        />
        <StatTile
          label="Should be in the drawer"
          value={formatCurrency(p?.expectedCash ?? 0)}
          tone="primary"
          loading={position.isLoading}
          icon={<AccountBalanceWalletOutlinedIcon />}
          iconTone="violet"
          tinted
        />
      </Box>

      <Paper variant="outlined" sx={{ p: 2.5 }}>
        {p?.isClosed ? (
          <Stack spacing={1}>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <LockOutlinedIcon sx={{ fontSize: 20, color: 'text.secondary' }} />
              <Typography sx={{ fontWeight: 700 }}>{formatDate(p.date)} is closed</Typography>
              <Chip
                size="small"
                variant="outlined"
                color={p.difference === 0 ? 'success' : 'warning'}
                label={
                  p.difference === 0
                    ? 'Counted exactly'
                    : `${formatCurrency(Math.abs(p.difference ?? 0))} ${(p.difference ?? 0) < 0 ? 'short' : 'over'}`
                }
              />
            </Stack>
            <Typography sx={{ fontSize: 14 }}>
              Expected {formatCurrency(p.expectedCash)}, counted {formatCurrency(p.countedCash ?? 0)}.
            </Typography>
            {p.reason ? (
              <Typography sx={{ fontSize: 13.5, color: 'text.secondary' }}>{p.reason}</Typography>
            ) : null}
            {/* A close is a statement about a moment — a later entry must not rewrite it. */}
            <Typography sx={{ fontSize: 12.5, color: 'text.secondary', mt: 0.5 }}>
              These figures are fixed as at the close. Anything entered afterwards for this day shows
              in the cash book but does not change what was counted.
            </Typography>
          </Stack>
        ) : (
          <Stack spacing={2}>
            <Typography sx={{ fontWeight: 700 }}>Count the drawer</Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: 'flex-end' }}>
              <TextField
                size="small"
                type="number"
                label="What is actually there"
                value={counted}
                onChange={(e) => setCounted(e.target.value)}
                sx={{ width: 220 }}
                slotProps={{ htmlInput: { min: 0, step: 'any' } }}
              />
              {counted !== '' ? (
                <Alert
                  severity={difference === 0 ? 'success' : 'warning'}
                  sx={{ flex: 1, py: 0.25 }}
                >
                  {difference === 0
                    ? 'Matches exactly.'
                    : `${formatCurrency(Math.abs(difference))} ${difference < 0 ? 'short' : 'over'} — say why below.`}
                </Alert>
              ) : null}
            </Stack>

            {needsReason ? (
              <TextField
                size="small"
                label="Why"
                required
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                placeholder={
                  difference < 0
                    ? 'Note missing, change given wrong, petty cash taken'
                    : 'A bill was keyed wrong, change kept back'
                }
                helperText="An unexplained surplus is as much a sign of a mis-keyed bill as a shortage is of a missing note"
              />
            ) : null}

            {can('CashDayClose') && (
              <Stack direction="row" sx={{ justifyContent: 'flex-end' }}>
                <Button
                  variant="contained"
                  startIcon={<LockOutlinedIcon />}
                  disabled={counted === '' || (needsReason && !reason.trim()) || close.isPending}
                  onClick={submit}
                >
                  {close.isPending ? 'Closing…' : `Close ${formatDate(p?.date)}`}
                </Button>
              </Stack>
            )}
          </Stack>
        )}
      </Paper>

      <MoneyMovementsCard fromDate={fromDate} toDate={toDate} />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ alignItems: { sm: 'flex-end' } }}>
        <Typography sx={{ fontWeight: 700, flex: 1 }}>Cash book</Typography>
        <TextField size="small" type="date" label="From" value={fromDate}
          onChange={(e) => setFromDate(e.target.value)} />
        <TextField size="small" type="date" label="To" value={toDate}
          onChange={(e) => setToDate(e.target.value)} />
      </Stack>

      <Paper variant="outlined">
        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small" sx={{ minWidth: 700 }}>
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 700, fontSize: 12.5 }}>Date</TableCell>
                <TableCell sx={{ fontWeight: 700, fontSize: 12.5 }}>Kind</TableCell>
                <TableCell sx={{ fontWeight: 700, fontSize: 12.5 }}>Particulars</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12.5 }}>In</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12.5 }}>Out</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12.5 }}>Balance</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              <TableRow>
                <TableCell colSpan={5} sx={{ fontSize: 12.5, fontWeight: 600 }}>
                  Brought forward
                </TableCell>
                <TableCell align="right" sx={{ fontSize: 12.5, fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                  {formatCurrency(book.data?.openingBalance ?? 0)}
                </TableCell>
              </TableRow>
              {(book.data?.entries ?? []).map((e, i) => (
                <TableRow key={`${e.date}-${e.kind}-${e.reference}-${i}`}>
                  <TableCell sx={{ fontSize: 12.5, whiteSpace: 'nowrap' }}>{formatDate(e.date)}</TableCell>
                  <TableCell sx={{ fontSize: 12.5 }}>
                    <Chip
                      size="small"
                      variant="outlined"
                      label={e.kind}
                      color={e.kind === 'Receipt' ? 'success' : e.kind === 'Day close' ? 'warning' : 'default'}
                    />
                  </TableCell>
                  <TableCell sx={{ fontSize: 13 }}>
                    {e.particulars}
                    {e.reference ? (
                      <Typography sx={{ fontSize: 11.5, color: 'text.secondary', fontFamily: 'monospace' }}>
                        {e.reference}
                      </Typography>
                    ) : null}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5, fontVariantNumeric: 'tabular-nums' }}>
                    {e.in ? formatCurrency(e.in) : ''}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5, fontVariantNumeric: 'tabular-nums' }}>
                    {e.out ? formatCurrency(e.out) : ''}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5, fontVariantNumeric: 'tabular-nums' }}>
                    {formatCurrency(e.balance)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>
        <Divider />
        <Stack direction="row" spacing={3} sx={{ justifyContent: 'flex-end', p: 2 }}>
          <Typography sx={{ fontWeight: 700 }}>Carried forward</Typography>
          <Typography sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
            {formatCurrency(book.data?.closingBalance ?? 0)}
          </Typography>
        </Stack>
      </Paper>
    </Stack>
  )
}
