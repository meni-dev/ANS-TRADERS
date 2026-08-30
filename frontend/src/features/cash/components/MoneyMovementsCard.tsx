import { describeError } from '@/lib/api/errors'
import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import AccountBalanceOutlinedIcon from '@mui/icons-material/AccountBalanceOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  MenuItem,
  Paper,
  Stack,
  Switch,
  FormControlLabel,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { DialogHeader } from '@/components/feedback/DialogHeader'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { useAuth } from '@/features/auth/AuthProvider'
import { formatCurrency, formatDate, todayIso } from '@/lib/format'
import { useCancelMoneyMovement, useCapitalSummary, useMoneyMovements, useRecordMoneyMovement } from '../hooks'
import { MONEY_MOVEMENTS } from '../types'

/** One sentence for the counter, whichever shape the failure came back in. */
const message = describeError

function RecordDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const record = useRecordMoneyMovement()
  const { notify } = useNotification()

  const [kind, setKind] = useState<string>('BankToCash')
  const [date, setDate] = useState(todayIso())
  const [amount, setAmount] = useState('')
  const [affectsCash, setAffectsCash] = useState(true)
  const [reference, setReference] = useState('')
  const [notes, setNotes] = useState('')
  const [error, setError] = useState<string | null>(null)

  const chosen = MONEY_MOVEMENTS.find((m) => m.value === kind)

  // Moving between the bank and the till is the till changing, by definition — there is nothing to
  // decide. Capital and drawings can go either way, so those are the only ones that ask.
  const cashIsImplied = kind === 'BankToCash' || kind === 'CashToBank' || kind === 'OpeningFloat'

  function close() {
    setAmount('')
    setReference('')
    setNotes('')
    setError(null)
    onClose()
  }

  async function submit() {
    setError(null)

    try {
      await record.mutateAsync({
        movementDate: date,
        kind,
        amount: Number(amount),
        affectsCash: cashIsImplied ? true : affectsCash,
        referenceNumber: reference.trim() || null,
        notes: notes.trim() || null,
      })
      notify('Recorded')
      close()
    } catch (caught) {
      setError(message(caught, 'Could not reach the server'))
    }
  }

  return (
    <Dialog open={open} onClose={close} maxWidth="xs" fullWidth>
      <DialogHeader
        title="Money in or out"
        subtitle="Cash that has no customer or supplier behind it"
        onClose={close}
      />
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}

          <TextField
            select
            label="What happened"
            value={kind}
            onChange={(event) => setKind(event.target.value)}
            helperText={chosen?.hint}
            fullWidth
          >
            {MONEY_MOVEMENTS.map((m) => (
              <MenuItem key={m.value} value={m.value}>
                {m.label}
              </MenuItem>
            ))}
          </TextField>

          <Stack direction="row" spacing={2}>
            <TextField
              type="date"
              label="Date"
              value={date}
              onChange={(event) => setDate(event.target.value)}
              slotProps={{ htmlInput: { max: todayIso() } }}
              fullWidth
            />
            <TextField
              type="number"
              label="Amount"
              value={amount}
              onChange={(event) => setAmount(event.target.value)}
              fullWidth
            />
          </Stack>

          {!cashIsImplied && (
            <FormControlLabel
              control={<Switch checked={affectsCash} onChange={(e) => setAffectsCash(e.target.checked)} />}
              label={affectsCash ? 'Through the till' : 'Straight to the bank'}
            />
          )}

          <TextField
            label="Reference"
            value={reference}
            onChange={(event) => setReference(event.target.value)}
            fullWidth
          />
          <TextField
            label="Note"
            value={notes}
            onChange={(event) => setNotes(event.target.value)}
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={close}>Cancel</Button>
        <Button
          variant="contained"
          onClick={() => void submit()}
          disabled={record.isPending || !amount || Number(amount) <= 0}
        >
          {record.isPending ? 'Saving…' : 'Record'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

/**
 * Where the till's money came from when it did not come from a sale.
 * <p>
 * Without this the cash book only ever goes down — card and UPI takings never reach the drawer,
 * while every cash expense leaves it.
 * </p>
 */
export function MoneyMovementsCard({ fromDate, toDate }: { fromDate: string; toDate: string }) {
  const { can } = useAuth()
  const mayMove = can('CapitalMovement')

  const { data: movements } = useMoneyMovements(fromDate, toDate)
  const { data: capital } = useCapitalSummary()
  const cancel = useCancelMoneyMovement()
  const { notify } = useNotification()

  const [adding, setAdding] = useState(false)

  async function remove(id: string) {
    try {
      await cancel.mutateAsync(id)
      notify('Cancelled')
    } catch (caught) {
      notify(message(caught, 'Could not cancel it'), 'error')
    }
  }

  return (
    <Paper variant="outlined" sx={{ p: 2.5 }}>
      <Stack spacing={2}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
          <AccountBalanceOutlinedIcon sx={{ fontSize: 20, color: 'text.disabled' }} />
          <Box sx={{ flexGrow: 1, minWidth: 0 }}>
            <Typography sx={{ fontSize: 15, fontWeight: 600 }}>Money in and out</Typography>
            <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
              The float you started with, money moved to and from the bank, and what you have put in
              or taken out yourself.
            </Typography>
          </Box>
          {mayMove && (
            <Button
              size="small"
              variant="contained"
              startIcon={<AddOutlinedIcon />}
              onClick={() => setAdding(true)}
              sx={{ flexShrink: 0 }}
            >
              Record
            </Button>
          )}
        </Stack>

        {capital && (
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
            <Chip size="small" label={`Opening float ${formatCurrency(capital.openingFloat)}`} />
            <Chip size="small" label={`Opening stock ${formatCurrency(capital.openingStockValue)}`} />
            <Chip size="small" label={`Capital in ${formatCurrency(capital.capitalIntroduced)}`} />
            <Chip size="small" label={`Drawings ${formatCurrency(capital.drawings)}`} />
            <Chip
              size="small"
              color="primary"
              label={`Net put in ${formatCurrency(capital.netInvested)}`}
            />
          </Stack>
        )}

        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Date</TableCell>
                <TableCell>What</TableCell>
                <TableCell>Reference</TableCell>
                <TableCell align="right">Amount</TableCell>
                <TableCell>Till</TableCell>
                {mayMove && <TableCell align="right" />}
              </TableRow>
            </TableHead>
            <TableBody>
              {movements?.map((m) => (
                <TableRow key={m.id} sx={{ opacity: m.isCancelled ? 0.5 : 1 }}>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDate(m.movementDate)}</TableCell>
                  <TableCell>
                    {m.kindLabel}
                    {m.notes && (
                      <Typography variant="caption" color="text.disabled" sx={{ display: 'block' }}>
                        {m.notes}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>{m.referenceNumber ?? '—'}</TableCell>
                  <TableCell align="right" sx={{ fontVariantNumeric: 'tabular-nums', fontWeight: 600 }}>
                    {formatCurrency(m.amount)}
                  </TableCell>
                  <TableCell>
                    {m.isCancelled ? (
                      <Chip size="small" label="Cancelled" />
                    ) : (
                      <Typography variant="caption" color="text.secondary">
                        {m.affectsCash ? 'Through the till' : 'Bank only'}
                      </Typography>
                    )}
                  </TableCell>
                  {mayMove && (
                    <TableCell align="right">
                      {!m.isCancelled && (
                        <Button size="small" color="error" onClick={() => void remove(m.id)}>
                          Cancel
                        </Button>
                      )}
                    </TableCell>
                  )}
                </TableRow>
              ))}
              {movements?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={mayMove ? 6 : 5}>
                    <Box sx={{ py: 3, textAlign: 'center' }}>
                      <Typography variant="body2" color="text.secondary">
                        Nothing recorded in this range.
                      </Typography>
                    </Box>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </Box>
      </Stack>

      <RecordDialog open={adding} onClose={() => setAdding(false)} />
    </Paper>
  )
}
