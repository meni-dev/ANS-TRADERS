import { describeError } from '@/lib/api/errors'
import AssignmentReturnOutlinedIcon from '@mui/icons-material/AssignmentReturnOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { computeLine } from '@/lib/documents/gst'
import { formatCurrency, formatDate, todayIso } from '@/lib/format'
import {
  Alert,
  Box,
  Button,
  Divider,
  FormControlLabel,
  MenuItem,
  Paper,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  useCreateCreditNote,
  useCreateDebitNote,
  useInvoiceReturnable,
  usePurchaseReturnable,
} from '../hooks'

const REFUND_MODES = [
  { value: 'Cash', label: 'Cash' },
  { value: 'Upi', label: 'UPI' },
  { value: 'BankTransfer', label: 'Bank Transfer' },
]

type ReturnFormPageProps = {
  /** Sales returns credit a customer; purchase returns debit a supplier. */
  side: 'sales' | 'purchase'
}

/**
 * Records goods coming back. Deliberately reached from the document itself rather than from a menu:
 * the counter is standing in front of the bill when the customer produces the part.
 */
export function ReturnFormPage({ side }: ReturnFormPageProps) {
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const isSales = side === 'sales'

  const invoiceReturnable = useInvoiceReturnable(isSales ? id : undefined)
  const purchaseReturnable = usePurchaseReturnable(isSales ? undefined : id)
  const document = isSales ? invoiceReturnable.data : purchaseReturnable.data

  // Taken from the original document, not recomputed: the note reverses the tax that was actually
  // charged, so the preview has to split it the same way.
  const isInterState = document?.isInterState ?? false
  const isLoading = isSales ? invoiceReturnable.isLoading : purchaseReturnable.isLoading

  const createCreditNote = useCreateCreditNote()
  const createDebitNote = useCreateDebitNote()
  const isPending = createCreditNote.isPending || createDebitNote.isPending

  const [noteDate, setNoteDate] = useState(todayIso())
  const [reason, setReason] = useState('')
  const [quantities, setQuantities] = useState<Record<string, string>>({})
  const [refundNow, setRefundNow] = useState(false)
  const [refundMode, setRefundMode] = useState('Cash')
  const [error, setError] = useState<string | null>(null)

  // Recomputed on every keystroke from the same rules the server uses, so what the counter reads
  // before saving is what the note will actually say.
  const total = useMemo(() => {
    if (!document) return 0

    return document.lines.reduce((sum, line) => {
      const quantity = Number(quantities[line.documentItemId] ?? 0)
      if (!quantity) return sum

      const amounts = computeLine(
        { quantity, rate: line.rate, discountPercent: line.discountPercent, gstRate: line.gstRate },
        isInterState,
      )

      return sum + amounts.lineTotal
    }, 0)
  }, [document, quantities, isInterState])

  const backTo = isSales ? `/billing/${id}` : `/purchases/${id}`

  async function submit() {
    if (!document) return
    setError(null)

    const lines = document.lines
      .map((line) => ({
        documentItemId: line.documentItemId,
        quantity: Number(quantities[line.documentItemId] ?? 0),
      }))
      .filter((line) => line.quantity > 0)

    const payload = {
      noteDate,
      reason,
      lines,
      refundAmount: refundNow ? total : undefined,
      refundMode: refundNow ? refundMode : undefined,
    }

    try {
      const note = isSales
        ? await createCreditNote.mutateAsync({ invoiceId: id!, payload })
        : await createDebitNote.mutateAsync({ purchaseId: id!, payload })

      navigate(isSales ? `/billing/returns/${note.id}` : `/purchases/returns/${note.id}`)
    } catch (caught) {
      setError(describeError(caught, 'Could not record this return'))
    }
  }

  if (isLoading) return <Typography sx={{ p: 3 }}>Loading…</Typography>

  if (!document) return <Alert severity="error">That document could not be found.</Alert>

  return (
    <Stack spacing={2.5}>
      <PageHeader
        title={isSales ? 'Goods coming back' : 'Goods going back'}
        icon={<AssignmentReturnOutlinedIcon />}
        iconTone="rose"
        caption={`${document.documentNumber} · ${document.partyName} · ${formatDate(document.documentDate)}`}
        onBack={() => navigate(backTo)}
        flush
      />

      {error ? <Alert severity="error">{error}</Alert> : null}

      {!document.canReturn ? (
        <Alert severity="info">{document.blockedReason}</Alert>
      ) : (
        <>
          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
              <TextField
                size="small"
                type="date"
                label="Date"
                value={noteDate}
                onChange={(event) => setNoteDate(event.target.value)}
              />
              <TextField
                size="small"
                label="Why are they coming back?"
                required
                value={reason}
                onChange={(event) => setReason(event.target.value)}
                placeholder="Wrong model, damaged, customer changed their mind"
                sx={{ flex: 1 }}
                // Required on the printed note by GST rules, and the first thing an auditor asks.
                helperText="Printed on the note"
              />
            </Stack>

            <Box sx={{ overflowX: 'auto' }}>
              <Table size="small" sx={{ minWidth: 640 }}>
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 700, fontSize: 12.5 }}>Item</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12.5 }}>
                      {isSales ? 'Sold' : 'Bought'}
                    </TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12.5 }}>
                      Already back
                    </TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12.5 }}>
                      Can still return
                    </TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12.5 }}>
                      Returning now
                    </TableCell>
                    <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12.5 }}>
                      Value
                    </TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {document.lines.map((line) => {
                    const entered = Number(quantities[line.documentItemId] ?? 0)
                    const overflowing = entered > line.quantityReturnable
                    const value = entered
                      ? computeLine(
                          {
                            quantity: entered,
                            rate: line.rate,
                            discountPercent: line.discountPercent,
                            gstRate: line.gstRate,
                          },
                          isInterState,
                        ).lineTotal
                      : 0

                    return (
                      <TableRow key={line.documentItemId}>
                        <TableCell sx={{ fontSize: 13 }}>
                          {line.itemName}
                          <Typography sx={{ fontSize: 11.5, color: 'text.secondary' }}>
                            {line.partNumber} · {formatCurrency(line.rate)}/{line.uqc}
                          </Typography>
                        </TableCell>
                        <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                          {line.quantitySold}
                        </TableCell>
                        <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                          {line.quantityReturned || '—'}
                        </TableCell>
                        <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                          {line.quantityReturnable}
                        </TableCell>
                        <TableCell align="right" sx={{ width: 130 }}>
                          <TextField
                            size="small"
                            type="number"
                            value={quantities[line.documentItemId] ?? ''}
                            disabled={line.quantityReturnable <= 0}
                            error={overflowing}
                            helperText={overflowing ? `Only ${line.quantityReturnable}` : undefined}
                            onChange={(event) =>
                              setQuantities((current) => ({
                                ...current,
                                [line.documentItemId]: event.target.value,
                              }))
                            }
                            slotProps={{ htmlInput: { min: 0, max: line.quantityReturnable, step: 'any' } }}
                          />
                        </TableCell>
                        <TableCell align="right" sx={{ fontSize: 13, fontVariantNumeric: 'tabular-nums' }}>
                          {value ? formatCurrency(value) : '—'}
                        </TableCell>
                      </TableRow>
                    )
                  })}
                </TableBody>
              </Table>
            </Box>

            <Divider sx={{ my: 2 }} />

            <Stack direction="row" sx={{ justifyContent: 'space-between' }}>
              <Typography sx={{ fontWeight: 700 }}>
                {isSales ? 'Credit note total' : 'Debit note total'}
              </Typography>
              <Typography sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
                {formatCurrency(total)}
              </Typography>
            </Stack>
            <Typography sx={{ fontSize: 12, color: 'text.secondary', textAlign: 'right' }}>
              Tax is worked out again on the server, from the rate on the original bill.
            </Typography>
          </Paper>

          <Paper variant="outlined" sx={{ p: 2.5 }}>
            <FormControlLabel
              control={
                <Switch checked={refundNow} onChange={(event) => setRefundNow(event.target.checked)} />
              }
              label={isSales ? 'Hand the money back now' : 'Supplier is paying it back now'}
            />
            <Typography sx={{ fontSize: 12.5, color: 'text.secondary' }}>
              {refundNow
                ? 'The cash movement is recorded alongside the note.'
                : `Leave this off and the credit stays on ${document.partyName}'s account, ready for the next bill.`}
            </Typography>

            {refundNow ? (
              <TextField
                select
                size="small"
                label="How"
                value={refundMode}
                onChange={(event) => setRefundMode(event.target.value)}
                sx={{ mt: 1.5, minWidth: 180 }}
              >
                {REFUND_MODES.map((mode) => (
                  <MenuItem key={mode.value} value={mode.value}>
                    {mode.label}
                  </MenuItem>
                ))}
              </TextField>
            ) : null}
          </Paper>

          <Stack direction="row" spacing={1.5} sx={{ justifyContent: 'flex-end' }}>
            <Button onClick={() => navigate(backTo)}>Cancel</Button>
            <Button
              variant="contained"
              disabled={isPending || total <= 0 || !reason.trim()}
              onClick={submit}
            >
              {isPending ? 'Recording…' : isSales ? 'Issue credit note' : 'Issue debit note'}
            </Button>
          </Stack>
        </>
      )}
    </Stack>
  )
}
