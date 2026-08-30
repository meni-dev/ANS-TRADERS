import { describeError } from '@/lib/api/errors'
import AssignmentReturnOutlinedIcon from '@mui/icons-material/AssignmentReturnOutlined'
import { PageHeader } from '@/components/layout/PageHeader'
import { ConfirmDialog } from '@/components/feedback/ConfirmDialog'
import { useNotification } from '@/components/feedback/NotificationProvider'
import { PrintStyles } from '@/features/billing/templates/PrintStyles'
import { useShopSettings } from '@/features/settings/hooks'
import { formatCurrency, formatDate } from '@/lib/format'
import BlockIcon from '@mui/icons-material/Block'
import PrintOutlinedIcon from '@mui/icons-material/PrintOutlined'
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
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  useCancelCreditNote,
  useCancelDebitNote,
  useCreditNote,
  useDebitNote,
} from '../hooks'

type ReturnNoteDetailPageProps = { side: 'sales' | 'purchase' }

/**
 * The printed note. It carries the original document's number and date because GSTR-1 reports a
 * credit note against the invoice it credits — a note that cannot be tied back to a bill is not one.
 */
export function ReturnNoteDetailPage({ side }: ReturnNoteDetailPageProps) {
  const navigate = useNavigate()
  const { id } = useParams<{ id: string }>()
  const { notify } = useNotification()
  const isSales = side === 'sales'

  const creditNote = useCreditNote(isSales ? id : undefined)
  const debitNote = useDebitNote(isSales ? undefined : id)
  const cancelCreditNote = useCancelCreditNote()
  const cancelDebitNote = useCancelDebitNote()

  const [confirmOpen, setConfirmOpen] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const shop = useShopSettings()
  const note = isSales ? creditNote.data : debitNote.data

  if (!note) return <Typography sx={{ p: 3 }}>Loading…</Typography>

  const number = isSales
    ? (note as { creditNoteNumber: string }).creditNoteNumber
    : (note as { debitNoteNumber: string }).debitNoteNumber

  const against = isSales
    ? (note as { invoiceNumber: string }).invoiceNumber
    : (note as { purchaseNumber: string }).purchaseNumber

  const partyName = isSales
    ? (note as { customerName: string }).customerName
    : (note as { supplierName: string }).supplierName

  const applied = isSales
    ? (note as { appliedToInvoiceAmount: number }).appliedToInvoiceAmount
    : (note as { appliedToPurchaseAmount: number }).appliedToPurchaseAmount

  const isCancelled = note.status === 'Cancelled'

  async function cancel() {
    setError(null)
    try {
      if (isSales) await cancelCreditNote.mutateAsync(id!)
      else await cancelDebitNote.mutateAsync(id!)
      notify(`${number} cancelled`, 'success')
      setConfirmOpen(false)
    } catch (caught) {
      setError(describeError(caught, 'Could not cancel this note'))
    }
  }

  return (
    <Stack spacing={2}>
      <PrintStyles page="A4" />

      <PageHeader
        title={number}
        icon={<AssignmentReturnOutlinedIcon />}
        iconTone="rose"
        badge={isCancelled ? <Chip size="small" label="Cancelled" /> : null}
        onBack={() => navigate(-1)}
        className="no-print"
        actions={
          <>
            <Button
              variant="outlined"
              startIcon={<PrintOutlinedIcon sx={{ fontSize: 18 }} />}
              onClick={() => window.print()}
            >
              Print
            </Button>
            {!isCancelled ? (
              <Button
                variant="outlined"
                color="error"
                startIcon={<BlockIcon sx={{ fontSize: 18 }} />}
                onClick={() => setConfirmOpen(true)}
              >
                Cancel
              </Button>
            ) : null}
          </>
        }
        flush
      />

      {error ? <Alert severity="error">{error}</Alert> : null}

      {isCancelled ? (
        <Alert severity="warning" className="no-print">
          This note has been cancelled. The goods went back out and the balance it credited has been
          restored — it is kept for the audit trail.
        </Alert>
      ) : null}

      <Paper variant="outlined" className="print-sheet" sx={{ p: { xs: 2, sm: 4 } }}>
        <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', mb: 2 }}>
          <Box>
            <Typography sx={{ fontSize: 17, fontWeight: 700 }}>{shop.data?.name}</Typography>
            {shop.data?.gstin ? (
              <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
                GSTIN {shop.data.gstin}
              </Typography>
            ) : null}
          </Box>
          <Box sx={{ textAlign: 'right' }}>
            <Typography
              sx={{ fontSize: 13, fontWeight: 700, textTransform: 'uppercase', letterSpacing: 0.6 }}
            >
              {isSales ? 'Credit Note' : 'Debit Note'}
            </Typography>
            <Typography sx={{ fontSize: 12.5, fontFamily: 'monospace' }}>{number}</Typography>
            <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
              {formatDate(note.noteDate)}
            </Typography>
          </Box>
        </Stack>

        <Divider sx={{ mb: 1.5 }} />

        <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', mb: 2 }}>
          <Box>
            <Typography sx={{ fontSize: 11.5, color: 'text.secondary', textTransform: 'uppercase' }}>
              {isSales ? 'Credit to' : 'Debit to'}
            </Typography>
            <Typography sx={{ fontSize: 14, fontWeight: 700 }}>{partyName}</Typography>
          </Box>
          <Box sx={{ textAlign: 'right' }}>
            <Typography sx={{ fontSize: 11.5, color: 'text.secondary', textTransform: 'uppercase' }}>
              Against
            </Typography>
            <Typography sx={{ fontSize: 13, fontFamily: 'monospace' }}>{against}</Typography>
            <Typography sx={{ fontSize: 12, color: 'text.secondary' }}>
              {formatDate(isSales ? (note as { invoiceDate: string }).invoiceDate : (note as { purchaseDate: string }).purchaseDate)}
            </Typography>
          </Box>
        </Stack>

        <Box sx={{ overflowX: 'auto' }}>
          <Table size="small" sx={{ minWidth: 660 }}>
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 700, fontSize: 12 }}>Item</TableCell>
                <TableCell sx={{ fontWeight: 700, fontSize: 12 }}>HSN</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12 }}>Qty</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12 }}>Rate</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12 }}>Taxable</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12 }}>GST</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700, fontSize: 12 }}>Total</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {note.items.map((item) => (
                <TableRow key={item.id}>
                  <TableCell sx={{ fontSize: 12.5 }}>
                    {item.itemName}
                    <Typography sx={{ fontSize: 11, color: 'text.secondary' }}>
                      {item.partNumber}
                    </Typography>
                  </TableCell>
                  <TableCell sx={{ fontSize: 12 }}>{item.hsn}</TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5, whiteSpace: 'nowrap' }}>
                    {item.quantity} {item.uqc}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5 }}>
                    {formatCurrency(item.rate)}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5 }}>
                    {formatCurrency(item.taxableAmount)}
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5, whiteSpace: 'nowrap' }}>
                    {item.gstRate}%
                  </TableCell>
                  <TableCell align="right" sx={{ fontSize: 12.5 }}>
                    {formatCurrency(item.lineTotal)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Box>

        <Divider sx={{ my: 1.5 }} />

        <Stack sx={{ alignItems: 'flex-end' }} spacing={0.5}>
          <Row label="Taxable value" value={note.taxableAmount} />
          {note.igstAmount > 0 ? (
            <Row label="IGST" value={note.igstAmount} />
          ) : (
            <>
              <Row label="CGST" value={note.cgstAmount} />
              <Row label="SGST" value={note.sgstAmount} />
            </>
          )}
          {note.roundOff !== 0 ? <Row label="Round off" value={note.roundOff} /> : null}
          <Row label={isSales ? 'Credit note total' : 'Debit note total'} value={note.grandTotal} bold />
        </Stack>

        <Divider sx={{ my: 1.5 }} />

        <Typography sx={{ fontSize: 12.5 }}>
          <strong>Reason:</strong> {note.reason}
        </Typography>

        {/* How the money was settled, which is the question the counter is actually asked. */}
        <Typography sx={{ fontSize: 12, color: 'text.secondary', mt: 0.75 }}>
          {applied > 0 ? `${formatCurrency(applied)} set against ${against}. ` : ''}
          {note.refundedAmount > 0 ? `${formatCurrency(note.refundedAmount)} paid back. ` : ''}
          {note.refundableAmount > 0
            ? `${formatCurrency(note.refundableAmount)} stands as credit on the account.`
            : ''}
        </Typography>
      </Paper>

      <ConfirmDialog
        open={confirmOpen}
        title={`Cancel ${number}?`}
        description="The goods go back the way they came, and the balance this note credited is restored. The note itself is kept."
        confirmLabel="Cancel the note"
        confirmColor="error"
        loading={cancelCreditNote.isPending || cancelDebitNote.isPending}
        onConfirm={cancel}
        onCancel={() => setConfirmOpen(false)}
      />
    </Stack>
  )
}

function Row({ label, value, bold }: { label: string; value: number; bold?: boolean }) {
  return (
    <Stack direction="row" spacing={4} sx={{ justifyContent: 'space-between', minWidth: 260 }}>
      <Typography sx={{ fontSize: 12.5, fontWeight: bold ? 700 : 400 }}>{label}</Typography>
      <Typography
        sx={{ fontSize: 12.5, fontWeight: bold ? 700 : 400, fontVariantNumeric: 'tabular-nums' }}
      >
        {formatCurrency(value)}
      </Typography>
    </Stack>
  )
}
